const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
function setup(confirm = true) {
  let calls = 0, message, loaded;
  const c = vm.createContext({ window: { confirm: text => { assert.match(text, /restore ₪20.00 to Safe/); return confirm; } },
    API: '/api/Expenses', $: () => ({ value: 'Personal' }), setMsg: (_, text) => message = text,
    apiSendJson: async (url, method) => { calls++; assert.equal(url, '/api/Expenses/7'); assert.equal(method, 'DELETE'); },
    getFriendlyMessage: e => e.message });
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../Expenses/expenses.js'), 'utf8'), c);
  c.loadList = async search => loaded = search;
  return { c, calls: () => calls, message: () => message, loaded: () => loaded };
}
test('cancelled expense deletion does not call the API', async () => {
  const s = setup(false), button = {}; await s.c.deleteExpense(7, 20, 'Safe', button); assert.equal(s.calls(), 0);
});
test('confirmed deletion refreshes the existing filter and blocks duplicate clicks', async () => {
  const s = setup(), button = {}; await s.c.deleteExpense(7, 20, 'Safe', button);
  await s.c.deleteExpense(7, 20, 'Safe', button);
  assert.equal(s.calls(), 1); assert.equal(s.loaded(), 'Personal'); assert.match(s.message(), /restored to Safe/);
});
test('failed deletion keeps the row available to retry and shows the error', async () => {
  const s = setup(), button = {}; s.c.apiSendJson = async () => { throw Error('Could not delete expense'); };
  await s.c.deleteExpense(7, 20, 'Safe', button);
  assert.equal(button.disabled, false); assert.equal(s.loaded(), undefined); assert.equal(s.message(), 'Could not delete expense');
});
