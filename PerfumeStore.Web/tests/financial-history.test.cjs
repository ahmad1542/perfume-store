const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../FinancialHistory/history.js'), 'utf8').replace(/initializeHistory\(\);\s*$/, '');
const common = fs.readFileSync(path.join(__dirname, '../assets/common.js'), 'utf8');
function setup(kind) {
  const nodes = {};
  const c = vm.createContext({ URLSearchParams, location: { search: '?kind=' + kind }, document: { getElementById: id => nodes[id] ??= {} } });
  vm.runInContext(common, c); vm.runInContext(source, c);
  return { c, nodes };
}
const report = { monthLabel: 'October 2026', openingBalance: 785, closingBalance: 785, currentAccountBalance: 785,
  moneyIn: 20, moneyOut: 20, revenue: 0, cost: 0, expenses: 20, netProfit: -20,
  rows: [{ date: '2026-10-19', recordedAt: '2026-10-19T12:00:00', sourceType: 'ReceiptVouchers', sourceId: '10002',
    description: 'Debt collected <script>', accountName: 'Safe', cashChange: 20, revenueChange: 0, costChange: 0, expenseChange: 0,
    profitChange: 0, runningBalance: 805, isReconstructed: true }] };

test('cash history shows account, receipt inflow, balance, and reconstructed status safely', () => {
  const { c, nodes } = setup('cash'); c.renderHistory(report);
  assert.match(nodes.rows.innerHTML, /Receipt voucher #10002/);
  assert.match(nodes.rows.innerHTML, /Safe/);
  assert.match(nodes.rows.innerHTML, /₪20\.00/);
  assert.match(nodes.rows.innerHTML, /₪805\.00/);
  assert.match(nodes.rows.innerHTML, /Reconstructed/);
  assert.match(nodes.rows.innerHTML, /&lt;script&gt;/);
  assert.ok(!nodes.rows.innerHTML.includes('<script>'));
});

test('profit history explains receipt collections without adding them to net profit', () => {
  const { c, nodes } = setup('profit'); c.renderHistory(report);
  assert.match(nodes.summary.innerHTML, /Net profit/);
  assert.match(nodes.summary.innerHTML, /₪-20\.00/);
  assert.match(nodes.rows.innerHTML, /no new profit/);
  assert.match(nodes.balanceNote.textContent, /original sale or expense month/);
});

test('balance discrepancy is visible and unknown source types cannot create arbitrary links', () => {
  const { c, nodes } = setup('cash'); c.renderHistory({ ...report, balanceDifference: 10 });
  assert.equal(nodes.reconciliationWarning.hidden, false);
  assert.match(nodes.reconciliationWarning.textContent, /₪10\.00/);
  assert.equal(c.historySourceLink({ sourceType: 'javascript:alert(1)', sourceId: '1' }), 'Transaction #1');
});

test('late responses cannot replace a newer history period', async () => {
  const { c, nodes } = setup('cash');
  for (const id of ['month', 'year', 'account']) nodes[id] = { value: id === 'month' ? '1' : id === 'year' ? '2026' : '' };
  c.APP_CONFIG = { API_BASE_URL: '/api' }; c.window = { history: { replaceState() {} } };
  let resolveFirst; c.apiGetJson = () => new Promise(resolve => resolveFirst = resolve);
  const first = c.loadHistory();
  let displayed; c.renderHistory = data => displayed = data;
  nodes.month.value = '2'; c.apiGetJson = async () => ({ monthLabel: 'February' });
  await c.loadHistory(); resolveFirst({ monthLabel: 'January' }); await first;
  assert.equal(displayed.monthLabel, 'February');
});
