const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const vm = require('node:vm');

for (const kind of ['Purchase', 'Sales']) {
  for (const action of ['create', 'edit']) {
    test(`${kind} ${action} sends the agreed unit prices with quantities`, () => {
      const fields = {
        Date: { value: '2026-10-05' }, SupplierId: { value: 'supplier' }, CustomerId: { value: 'customer' },
        AmountPaid: { value: '30' }, MoneyAccountId: { value: '1' }, HasDebt: { checked: false },
        DebtAmount: { value: '' }, DebtNotes: { value: '' }, Notes: { value: '' }
      };
      const context = vm.createContext({ window: {}, $: id => fields[id] });
      vm.runInContext(readFileSync(join(__dirname, `../${kind}Invoices/${action}.js`), 'utf8'), context);
      context.window._items = [{ productId: 7, qty: 2, unitPrice: 25 }, { productId: 8, qty: 1, unitPrice: 0 }];
      const body = JSON.parse(JSON.stringify(context.buildBodyFromForm()));
      assert.deepEqual(body.Products, { 7: 2, 8: 1 });
      assert.deepEqual(body[kind === 'Sales' ? 'UnitPrices' : 'UnitCosts'], { 7: 25, 8: 0 });
      assert.equal(body.AmountPaid, 30);
    });
  }
}

test('sales auto-fill uses customer price while retaining existing payment auto-fill behavior', () => {
  const amount = { value: '', dispatchEvent() {} };
  const context = vm.createContext({ window: {}, $: () => amount, Event: function Event() {} });
  vm.runInContext(readFileSync(join(__dirname, '../SalesInvoices/create.js'), 'utf8'), context);
  context.window._productPrices = { 7: 30 };
  context.window._items = [{ productId: 7, qty: 2, unitPrice: 25 }];
  context.updateAmountPaidFromItems();
  assert.equal(amount.value, 50);
});

for (const action of ['create', 'edit']) {
  test(`Inventory ${action} distinguishes missing stock cost from a free unit`, () => {
    const fields = { ProductID: { value: '7' }, Quantity: { value: '6' }, AddedUnitCost: { value: '' } };
    const context = vm.createContext({ window: {}, $: id => fields[id] });
    vm.runInContext(readFileSync(join(__dirname, `../Inventory/${action}.js`), 'utf8'), context);
    assert.equal(context.buildBodyFromForm().AddedUnitCost, null);
    fields.AddedUnitCost.value = '0';
    assert.equal(context.buildBodyFromForm().AddedUnitCost, 0);
    fields.AddedUnitCost.value = '780';
    assert.equal(context.buildBodyFromForm().AddedUnitCost, 780);
  });
}

for (const [location, expected] of [
  [{ protocol: 'http:', port: '8080', hostname: 'localhost', origin: 'http://localhost:8080' }, 'http://localhost:8080/api'],
  [{ protocol: 'http:', port: '8080', hostname: '192.168.1.4', origin: 'http://192.168.1.4:8080' }, 'http://192.168.1.4:8080/api'],
  [{ protocol: 'http:', port: '5500', hostname: 'localhost', origin: 'http://localhost:5500' }, 'https://localhost:7209/api']
]) {
  test(`API configuration supports ${location.hostname}:${location.port}`, () => {
    const context = vm.createContext({ window: { location } });
    vm.runInContext(readFileSync(join(__dirname, '../assets/api-config.js'), 'utf8'), context);
    assert.equal(context.window.APP_CONFIG.API_BASE_URL, expected);
  });
}
