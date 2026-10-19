const historyParams = new URLSearchParams(location.search);
const historyKind = historyParams.get('kind') === 'profit' ? 'profit' : 'cash';
const cashHistory = historyKind === 'cash';
let historyRequest = 0;
const historyMoney = value => '₪' + Number(value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const historyDate = value => new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
const historySourceNames = { SalesInvoices: 'Sale invoice', PurchaseInvoices: 'Purchase invoice', ReceiptVouchers: 'Receipt voucher', PaymentVouchers: 'Payment voucher', Expenses: 'Expense', MoneyAccounts: 'Money account', MoneyTransactions: 'Transfer', Debts: 'Debt', Products: 'Product', Inventory: 'Inventory' };

function historySourceLink(row) {
  const label = `${historySourceNames[row.sourceType] || 'Transaction'}${row.sourceId ? ' #' + row.sourceId : ''}`;
  if (!historySourceNames[row.sourceType] || !row.sourceId) return escapeHtml(label);
  return `<a href="../${row.sourceType}/view.html?id=${encodeURIComponent(row.sourceId)}">${escapeHtml(label)}</a>`;
}

function historyAmount(value, signed = false, invertColor = false) {
  const n = Number(value || 0);
  const impact = invertColor ? -n : n;
  return `<td class="amount ${signed && impact < 0 ? 'negative' : signed && impact > 0 ? 'positive' : ''}">${signed && n > 0 ? '+' : ''}${historyMoney(n)}</td>`;
}

function renderHistory(data) {
  $('reconciliationWarning').hidden = !cashHistory || !data.balanceDifference;
  $('reconciliationWarning').textContent = `The saved account balance differs from recorded history by ${historyMoney(data.balanceDifference)}. Review the account before relying on these totals.`;
  const stats = cashHistory
    ? [['Opening balance', data.openingBalance], ['Money in', data.moneyIn], ['Money out', data.moneyOut], ['Closing balance', data.closingBalance]]
    : [['Sales revenue', data.revenue], ['Cost of goods sold', data.cost], ['Expenses', data.expenses], ['Net profit', data.netProfit]];
  $('summary').innerHTML = stats.map(([label, value]) => `<div class="stat"><span>${label}</span><strong class="${Number(value) < 0 ? 'negative' : ''}">${historyMoney(value)}</strong></div>`).join('');
  $('caption').textContent = `${data.monthLabel} • ${data.rows.length} entries • Oldest first`;
  $('balanceNote').textContent = cashHistory
    ? `Opening balance + money in − money out = closing balance for this month. Current saved balance: ${historyMoney(data.currentAccountBalance)}. Transfers appear on both accounts and do not change combined cash.`
    : 'Revenue − cost of goods sold − expenses = net profit. Debt collections are shown for context with zero profit impact. Profit corrections affect the original sale or expense month.';
  $('trackingNote').textContent = data.trackingStartedAt
    ? `Detailed change tracking began ${historyDate(data.trackingStartedAt + (data.trackingStartedAt.endsWith('Z') ? '' : 'Z'))}. “Reconstructed” entries describe records that existed before tracking began.`
    : 'Detailed change tracking is active. Reconstructed entries describe records that existed before tracking began.';
  const headings = cashHistory ? ['Date', 'Transaction', 'Account', 'Money in', 'Money out', 'Balance'] : ['Date', 'Transaction', 'Revenue change', 'Cost change', 'Expense change', 'Profit change', 'Month profit'];
  $('columns').innerHTML = '<tr>' + headings.map((x, i) => `<th${i >= (cashHistory ? 3 : 2) ? ' class="amount"' : ''}>${x}</th>`).join('') + '</tr>';
  $('rows').innerHTML = data.rows.map(row => {
    let note = '';
    if (row.isReconstructed) note += '<span class="badge">Reconstructed</span> ';
    if (row.isEstimated) note += '<span class="badge">Estimated</span>';
    const recorded = row.isReconstructed ? '' : `<small>Recorded ${escapeHtml(new Date(row.recordedAt.endsWith('Z') ? row.recordedAt : row.recordedAt + 'Z').toLocaleString())}</small>`;
    const description = row.description.replace(/SalesInvoices|PurchaseInvoices|ReceiptVouchers|PaymentVouchers|MoneyAccounts|MoneyTransactions|Expenses|Debts|Products|Inventory/g, value => historySourceNames[value] || value);
    const transaction = `<td>${historySourceLink(row)}<small>${escapeHtml(description)}</small>${!cashHistory && row.cashChange ? `<small>Cash ${row.cashChange > 0 ? '+' : ''}${historyMoney(row.cashChange)}; no new profit</small>` : ''}${recorded}${note}</td>`;
    const content = `<td>${escapeHtml(historyDate(row.date))}</td>${transaction}${cashHistory
      ? `<td>${escapeHtml(row.accountName || '—')}</td>${historyAmount(Math.max(0, row.cashChange))}${historyAmount(Math.max(0, -row.cashChange))}${historyAmount(row.runningBalance)}`
      : `${historyAmount(row.revenueChange, true)}${historyAmount(row.costChange, true, true)}${historyAmount(row.expenseChange, true, true)}${historyAmount(row.profitChange, true)}${historyAmount(row.runningBalance)}`}`;
    let column = 0;
    return '<tr>' + content.replace(/<td(?=[ >])/g, tag => tag + ` data-label="${headings[column++]}"`) + '</tr>';
  }).join('') || `<tr><td class="empty" colspan="${headings.length}">No transactions in this month.</td></tr>`;
}

async function loadHistory() {
  const request = ++historyRequest;
  $('message').textContent = 'Loading history…'; $('message').className = '';
  const params = new URLSearchParams({ year: $('year').value, month: $('month').value });
  if (cashHistory && $('account').value) params.set('accountId', $('account').value);
  const navigation = new URLSearchParams(params); navigation.delete('accountId');
  $('cashTab').href = `?kind=cash&${navigation}`; $('profitTab').href = `?kind=profit&${navigation}`;
  const address = new URLSearchParams(params); address.set('kind', historyKind);
  window.history.replaceState(null, '', `?${address}`);
  try {
    const data = await apiGetJson(`${APP_CONFIG.API_BASE_URL}/FinancialHistory/${historyKind}?${params}`);
    if (request !== historyRequest) return;
    renderHistory(data); $('message').textContent = '';
  } catch (error) {
    if (request !== historyRequest) return;
    $('summary').innerHTML = ''; $('rows').innerHTML = ''; $('balanceNote').textContent = '';
    $('message').className = 'error'; $('message').textContent = 'Could not load history. ' + (getValidationMessageFromHttpError(error) || 'Please try again.');
  }
}

async function initializeHistory() {
  const now = new Date();
  $('title').textContent = cashHistory ? 'Cash History' : 'Profit History';
  document.title = `${$('title').textContent} - Memento Perfume`;
  $('explanation').textContent = cashHistory ? 'Follow money received, money paid, and the balance in each account.' : 'See how sales, FIFO costs, expenses, and corrections contribute to monthly profit.';
  $(cashHistory ? 'cashTab' : 'profitTab').setAttribute('aria-current', 'page');
  $('accountFilter').hidden = !cashHistory;
  for (let m = 1; m <= 12; m++) $('month').add(new Option(new Date(2021, m - 1, 1).toLocaleString(undefined, { month: 'long' }), m));
  for (let y = now.getFullYear(); y >= 2021; y--) $('year').add(new Option(y, y));
  const month = Number(historyParams.get('month')), year = Number(historyParams.get('year'));
  $('month').value = month >= 1 && month <= 12 ? month : now.getMonth() + 1;
  $('year').value = year >= 2021 && year <= now.getFullYear() ? year : now.getFullYear();
  try {
    const accounts = await apiGetJson(`${APP_CONFIG.API_BASE_URL}/MoneyAccounts`);
    accounts.forEach(x => $('account').add(new Option(x.accountName, x.id)));
    if (accounts.some(x => String(x.id) === historyParams.get('accountId'))) $('account').value = historyParams.get('accountId');
  } catch {
    $('message').className = 'error'; $('message').textContent = 'Could not load accounts. Refresh to try again.'; return;
  }
  ['month', 'year', 'account'].forEach(id => $(id).addEventListener('change', loadHistory));
  $('refresh').addEventListener('click', loadHistory);
  await loadHistory();
}

initializeHistory();
