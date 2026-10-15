// Run only against a disposable test stack: this creates test records.
// node scripts/docker-smoke.cjs http://127.0.0.1:18080 --disposable
const assert = require('node:assert/strict');

const base = process.argv[2];
if (!base || !process.argv.includes('--disposable')) {
  throw new Error('Supply a disposable stack URL and --disposable. Do not use your real store database.');
}
const suffix = Date.now().toString();
const today = new Date().toISOString().split('T')[0];

async function request(path, method = 'GET', body, status = 200) {
  const response = await fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json' },
    body: body == null ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(15000)
  });
  const text = await response.text();
  assert.equal(response.status, status, `${method} ${path}: ${text}`);
  return { body: text ? JSON.parse(text) : null, location: response.headers.get('location') };
}

async function create(resource, body) {
  const result = await request(`/api/${resource}`, 'POST', body, 201);
  assert.ok(result.location);
  return result.location.split('/').at(-1);
}

async function run() {
  assert.equal((await request('/health')).body.status, 'healthy');
  const home = await fetch(base, { signal: AbortSignal.timeout(15000) });
  assert.equal(home.status, 200);
  assert.ok((await home.text()).includes('Dashboard'));
  const form = await fetch(`${base}/Inventory/edit.html`);
  assert.ok((await form.text()).includes('AddedUnitCost'));
  const category = Number(await create('ProductCategories', { Name: `Smoke ${suffix}` }));
  const product = Number(await create('Products', {
    Name: `Naxos smoke ${suffix}`, Capacity: 100, CostPrice: 750, SalePrice: 900,
    ProductCategoryID: category
  }));
  const supplier = await create('Suppliers', { Name: `Smoke supplier ${suffix}`, Phone: `05${suffix.slice(-8)}` });
  const account = Number(await create('MoneyAccounts', { AccountName: `Smoke ${suffix}`, CurrentBalance: 10000 }));
  await request('/api/Inventory', 'POST', { ProductID: product, Quantity: 4 }, 400);
  await create('Inventory', { ProductID: product, Quantity: 4, AddedUnitCost: 750 });
  const invoiceBody = {
    Date: today, AmountPaid: 1560, MoneyAccountId: account,
    HasDebt: false, DebtAmount: 0, SupplierId: supplier,
    Products: { [product]: 2 }, UnitCosts: { [product]: 780 }
  };
  const receipt = await create('PurchaseInvoices', invoiceBody);
  const saleBody = {
    Date: today, AmountPaid: 4500, MoneyAccountId: account,
    HasDebt: false, DebtAmount: 0,
    Products: { [product]: 5 }, UnitPrices: { [product]: 900 }
  };
  const sale = await create('SalesInvoices', saleBody);
  let saved = (await request(`/api/SalesInvoices/${sale}`)).body;
  assert.equal(saved.products[0].costTotal, 3780);
  assert.equal(saved.products[0].grossProfit, 720);
  let stock = (await request(`/api/Inventory/${product}`)).body;
  assert.equal(stock.quantity, 1);
  assert.equal(stock.stockCost, 780);
  assert.equal((await request(`/api/Products/${product}`)).body.salePrice, 900);
  assert.equal((await request(`/api/Products/${product}`)).body.costPrice, 750);
  await request(`/api/PurchaseInvoices/${receipt}`, 'PATCH', {
    ...invoiceBody, UnitCosts: { [product]: 800 }
  }, 400);
  await request(`/api/PurchaseInvoices/${receipt}`, 'PATCH', {
    ...invoiceBody, AmountPaid: 1600, UnitCosts: { [product]: 800 }
  }, 204);
  saved = (await request(`/api/SalesInvoices/${sale}`)).body;
  assert.equal(saved.products[0].costTotal, 3800);
  assert.equal(saved.products[0].unitPrice, 900);
  await request(`/api/PurchaseInvoices/${receipt}`, 'PATCH', {
    ...invoiceBody, Products: { [product]: 0 }
  }, 400);
  assert.equal((await request(`/api/Inventory/${product}`)).body.quantity, 1);
  await request(`/api/SalesInvoices/${sale}`, 'PATCH', {
    ...saleBody, Products: { [product]: 6 }, UnitPrices: { [product]: 30 }
  }, 400);
  await request(`/api/SalesInvoices/${sale}`, 'PATCH', {
    ...saleBody, AmountPaid: 180, Products: { [product]: 6 }, UnitPrices: { [product]: 30 }
  }, 204);
  await request(`/api/SalesInvoices/${sale}`, 'PATCH', {
    ...saleBody, AmountPaid: 150, Products: { [product]: 6 }, UnitPrices: { [product]: 25 }
  }, 204);
  saved = (await request(`/api/SalesInvoices/${sale}`)).body;
  assert.equal(saved.products[0].unitPrice, 25);
  assert.equal(saved.products[0].costTotal, 4600);
  stock = (await request(`/api/Inventory/${product}`)).body;
  assert.equal(stock.quantity, 0);
  assert.equal(stock.stockCost, 0);
  const missing = await request('/api/Products/2147483647', 'GET', null, 404);
  assert.ok(missing.body.traceId);
  console.log(`Docker/SQL Server checks passed: migrations, hosted pages, FIFO, prices, corrections, rollback, errors. Product ${product}.`);
}

run().catch(error => { console.error(error); process.exitCode = 1; });
