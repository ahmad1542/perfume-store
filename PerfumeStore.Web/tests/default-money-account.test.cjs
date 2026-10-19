const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const c = vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(__dirname, '../assets/common.js'), 'utf8'), c);
const accountSelect = () => ({ value: '', disabled: false, options: [
  { value: '', textContent: '-- Select --' }, { value: '1', textContent: 'Bank' }, { value: '37', textContent: 'Safe' }
] });

test('Safe default resolves the loaded account ID rather than assuming ID 1', () => {
  const select = accountSelect(); c.selectSafeMoneyAccount(select); assert.equal(select.value, '37');
});
test('explicit Bank choice is retained when payment input changes', () => {
  const select = accountSelect(); select.value = '1'; c.selectSafeMoneyAccount(select); assert.equal(select.value, '1');
});
test('missing Safe leaves the account unselected', () => {
  const select = accountSelect(); select.options.pop(); c.selectSafeMoneyAccount(select); assert.equal(select.value, '');
});
test('zero-payment invoices stay unselected until the account is enabled', () => {
  const select = accountSelect(); select.disabled = true; c.selectSafeMoneyAccount(select); assert.equal(select.value, '');
  select.disabled = false; c.selectSafeMoneyAccount(select); assert.equal(select.value, '37');
});
