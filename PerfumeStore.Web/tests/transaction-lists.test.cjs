const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
class Element {
  constructor() { this.children = []; this.events = {}; this.value = ''; this.style = {}; this.hidden = false; this._text = ''; }
  appendChild(child) { this.children.push(child); return child; }
  append(...children) { this.children.push(...children); }
  replaceChildren(...children) { this.children = children; this._text = ''; }
  get textContent() { return this._text + this.children.map(x => x.textContent).join(' '); }
  set textContent(value) { this._text = String(value); this.children = []; }
  set innerHTML(value) { this._html = value; this.children = []; }
  get innerHTML() { return this._html || ''; }
  addEventListener(name, fn) { this.events[name] = fn; }
  querySelector() { return new Element(); }
}
function setup({ query = '', dates = true } = {}) {
  const nodes = Object.fromEntries(['rows','tableHead','emptyState','tableCard','countChip','listTotals','pageMsg','searchInput',...(dates?['fromDateInput','toDateInput','currentMonthButton','allDatesButton']:[])].map(id => [id,new Element()]));
  const c = vm.createContext({ document: { getElementById: id => nodes[id], createElement: () => new Element(), querySelectorAll: () => [] },
    location: { search: query }, window: {}, URLSearchParams, setTimeout: fn => { fn(); return 1; }, clearTimeout() {},
    $: id => nodes[id], setMsg: (_, text) => nodes.pageMsg.textContent = text, escapeHtml: String, formatDateOnly: String,
    API: '/api/Test', console });
  vm.runInContext(fs.readFileSync(path.join(__dirname,'../assets/transaction-lists.js'),'utf8'),c);
  c.list = vm.runInContext('TransactionList',c);
  return { c, nodes };
}
test('local month boundaries include leap years and December; defaults exist before requests', () => {
  const { c, nodes } = setup();
  assert.equal(nodes.fromDateInput.value.slice(8),'01');
  assert.deepEqual(Array.from(c.list.thisMonth(new Date(2024,1,15))),['2024-02-01','2024-02-29']);
  assert.deepEqual(Array.from(c.list.thisMonth(new Date(2026,11,31))),['2026-12-01','2026-12-31']);
  assert.match(c.list.url('/api/Expenses','rent'),/fromDate=/);
  assert.match(c.list.url('/api/Expenses','rent'),/search=rent/);
});
test('All dates and This month preserve the current search', () => {
  const { c, nodes } = setup(); let search; c.window.loadList = value => search = value;
  nodes.searchInput.value = 'rent'; nodes.allDatesButton.events.click();
  assert.equal(nodes.fromDateInput.value,''); assert.equal(nodes.toDateInput.value,''); assert.equal(search,'rent');
  assert.ok(!c.list.url('/api/Expenses','rent').includes('fromDate'));
  nodes.currentMonthButton.events.click(); assert.ok(nodes.fromDateInput.value); assert.equal(search,'rent');
});
test('expense totals use only the loaded rows and display each category safely', () => {
  const { c, nodes } = setup();
  c.list.render('Expenses',[{amount:0.1,expenseTypeName:'Shop'},{amount:0.2,expenseTypeName:'Shop'},{amount:4,expenseTypeName:'<img onerror=alert(1)>'}]);
  const text = nodes.listTotals.textContent;
  assert.match(text,/₪4\.30/); assert.match(text,/Shop ₪0\.30/); assert.match(text,/<img onerror=alert\(1\)>/);
  c.list.render('Expenses',[]); assert.equal(nodes.listTotals.hidden,false); assert.match(nodes.listTotals.textContent,/₪0\.00/);
});
test('invoice totals are independent of paid amounts and remaining debt', () => {
  const { c, nodes } = setup();
  c.list.render('SalesInvoices',[{invoiceTotal:18,amountPaid:4,debtAmount:2}]);
  assert.match(nodes.listTotals.textContent,/Invoice total ₪18\.00/);
  assert.match(nodes.listTotals.textContent,/Paid on invoices ₪4\.00/);
  assert.match(nodes.listTotals.textContent,/Remaining debt ₪2\.00/);
});
test('receivable and payable totals exclude settled debts without netting the two directions', () => {
  const { c, nodes } = setup();
  c.list.render('Debts',[{amount:10,direction:1},{amount:20,direction:2},{amount:5,direction:1,isDeleted:true}]);
  assert.match(nodes.listTotals.textContent,/Loaded debt amounts ₪35\.00/);
  assert.match(nodes.listTotals.textContent,/Open receivables ₪10\.00/);
  assert.match(nodes.listTotals.textContent,/Open payables ₪20\.00/);
});

test('active product filters show zero matching totals for empty invoice results', () => {
  const {c,nodes}=setup(); c.RecordFilters={hasLineFilters:()=>true};
  c.list.render('SalesInvoices',[]);assert.match(nodes.listTotals.textContent,/Matching products only ₪0\.00/);
});
test('invalid ranges clear old totals and skip loading; request tokens reject stale responses', () => {
  const { c, nodes } = setup();
  const old = c.list.begin(), latest = c.list.begin();
  assert.equal(c.list.current(old),false); assert.equal(c.list.current(latest),true);
  nodes.fromDateInput.value='2026-11-01'; nodes.toDateInput.value='2026-10-31';
  assert.equal(c.list.begin(),null); assert.equal(nodes.listTotals.hidden,true);
  assert.match(nodes.pageMsg.textContent,/From date/);
});
for (const kind of ['Expenses','SalesInvoices','PurchaseInvoices','Debts','PaymentVouchers','ReceiptVouchers','MoneyTransactions','MoneyAccounts']) {
  test(`${kind} first list request sends its visible date bounds and clears totals on errors`, async () => {
    const { c, nodes } = setup({dates:kind!=='MoneyAccounts'}); let url;
    c.fetch = async value => { url=value; return {ok:true,json:async()=>[]}; };
    c.apiGetJson = async value => {url=value; return [];};
    c.getFriendlyMessage=e=>e.message;
    vm.runInContext(fs.readFileSync(path.join(__dirname,`../${kind}/${kind.toLowerCase()}.js`),'utf8'),c);
    await c.loadList('test');
    const params = new URL(url,'https://example.test').searchParams;
    assert.equal(params.has('fromDate'),kind!=='MoneyAccounts');
    if(kind!=='MoneyAccounts') assert.equal(params.get('fromDate'),nodes.fromDateInput.value);
    assert.equal(params.get('search'),'test'); assert.equal(nodes.listTotals.hidden,false);
    c.fetch=async()=>{throw Error('Failed')}; c.apiGetJson=c.fetch;
    await c.loadList('test'); assert.equal(nodes.listTotals.hidden,true);
  });
}
