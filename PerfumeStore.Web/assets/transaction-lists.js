// Shared only by transaction/account list pages, never by create/edit forms.
const TransactionList = (() => {
  let request = 0;
  const node = id => document.getElementById(id);
  const field = (row, name) => row[name] ?? row[name[0].toUpperCase() + name.slice(1)];
  const sum = (rows, name) => rows.reduce((total, row) => total + Math.round(Number(field(row, name) || 0) * 100), 0) / 100;
  const money = value => '₪' + value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const localDate = date => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  function thisMonth(now = new Date()) {
    return [localDate(new Date(now.getFullYear(), now.getMonth(), 1)), localDate(new Date(now.getFullYear(), now.getMonth() + 1, 0))];
  }
  function initializeDates() {
    const from = node('fromDateInput'), to = node('toDateInput');
    if (!from || !to) return;
    const params = new URLSearchParams(location.search);
    const [start, end] = thisMonth();
    from.value = params.has('fromDate') ? params.get('fromDate') : params.has('allDates') ? '' : start;
    to.value = params.has('toDate') ? params.get('toDate') : params.has('allDates') ? '' : end;
    let timer;
    const reload = () => {
      clearTimeout(timer);
      timer = setTimeout(() => window.loadList?.(node('searchInput')?.value || ''), 250);
    };
    from.addEventListener('change', reload); to.addEventListener('change', reload);
    node('currentMonthButton')?.addEventListener('click', () => {
      [from.value, to.value] = thisMonth(); reload();
    });
    node('allDatesButton')?.addEventListener('click', () => {
      from.value = ''; to.value = ''; reload();
    });
  }
  function addDates(params) {
    for (const [id, key] of [['fromDateInput', 'fromDate'], ['toDateInput', 'toDate']]) {
      const value = node(id)?.value;
      if (value) params.set(key, value);
    }
    if (typeof RecordFilters !== 'undefined') RecordFilters.append(params, false);
    return params;
  }
  function url(base, search, extra = {}) {
    const params = addDates(new URLSearchParams(extra));
    if (search?.trim()) params.set('search', search.trim());
    if (typeof RecordFilters !== 'undefined') RecordFilters.append(params);
    return params.size ? `${base}?${params}` : base;
  }
  function begin() {
    const token = ++request;
    const totals = node('listTotals'); if (totals) { totals.hidden = true; totals.replaceChildren(); }
    const from = node('fromDateInput')?.value, to = node('toDateInput')?.value;
    if (from && to && from > to) {
      const rows = node('rows'); if (rows) rows.replaceChildren();
      const chip = node('countChip'); if (chip) chip.textContent = '0';
      const card = node('tableCard'); if (card) card.style.display = 'none';
      const empty = node('emptyState'); if (empty) empty.style.display = 'none';
      setMsg('pageMsg', 'From date must be on or before To date.', true);
      return null;
    }
    return token;
  }
  function current(token) { return token === request; }
  function render(kind, rows) {
    const panel = node('listTotals'); if (!panel) return;
    let totals;
    switch (kind) {
      case 'SalesInvoices': case 'PurchaseInvoices':
        totals = [['Invoice total', sum(rows, 'invoiceTotal')], ['Paid on invoices', sum(rows, 'amountPaid')], ['Remaining debt', sum(rows, 'debtAmount')]]; break;
      case 'Debts': {
        const open = rows.filter(x => !field(x, 'isDeleted'));
        totals = [['Loaded debt amounts', sum(rows, 'amount')], ['Open receivables', sum(open.filter(x => Number(field(x, 'direction')) === 1), 'amount')], ['Open payables', sum(open.filter(x => Number(field(x, 'direction')) === 2), 'amount')]]; break;
      }
      case 'MoneyTransactions': totals = [['Total transferred', sum(rows, 'transferAmount')]]; break;
      case 'MoneyAccounts': totals = [['Total account balances', sum(rows, 'currentBalance')]]; break;
      default: totals = [[kind === 'Expenses' ? 'Total expenses' : kind === 'ReceiptVouchers' ? 'Total received' : 'Total paid', sum(rows, 'amount')]];
    }
    panel.replaceChildren();
    const title = document.createElement('h2'); title.textContent = 'Totals for loaded records'; panel.appendChild(title);
    const caption = document.createElement('p'); caption.className = 'list-totals-caption';
    const from = node('fromDateInput')?.value, to = node('toDateInput')?.value;
    caption.textContent = `${rows.length} record(s) • ${from || to ? `${from || 'Beginning'} to ${to || 'present'}` : 'All dates'} • Current search and filters`;
    panel.appendChild(caption);
    const grid = document.createElement('div'); grid.className = 'list-totals-grid';
    function card(label, amount) {
      const item = document.createElement('div'); item.className = 'list-total';
      const text = document.createElement('span'); text.textContent = label;
      const value = document.createElement('strong'); value.textContent = money(amount);
      item.append(text, value); grid.appendChild(item);
    }
    totals.forEach(([label, amount]) => card(label, amount));
    if (rows.some(x => field(x, 'matchingProductsTotal') != null) || (['SalesInvoices','PurchaseInvoices'].includes(kind) && typeof RecordFilters !== 'undefined' && RecordFilters.hasLineFilters())) card('Matching products only', sum(rows, 'matchingProductsTotal'));
    panel.appendChild(grid);
    if (kind === 'Expenses' && rows.length) {
      const categories = new Map();
      for (const row of rows) {
        const name = field(row, 'expenseTypeName') || 'Other';
        if (!categories.has(name)) categories.set(name, []);
        categories.get(name).push(row);
      }
      const breakdown = document.createElement('div'); breakdown.className = 'list-totals-breakdown';
      for (const [name, items] of categories) {
        const line = document.createElement('p'); const label = document.createElement('span'); const value = document.createElement('strong');
        label.textContent = name; value.textContent = money(sum(items, 'amount')); line.append(label, value); breakdown.appendChild(line);
      }
      panel.appendChild(breakdown);
    }
    if (kind === 'SalesInvoices' || kind === 'PurchaseInvoices' || kind === 'Debts') {
      const note = document.createElement('p'); note.className = 'list-totals-caption';
      note.textContent = kind === 'Debts'
        ? 'Dates refer to the debt date. Use All dates to include older unpaid debts. Open totals exclude settled records.'
        : 'Invoice total uses saved line prices. Paid on invoices excludes later vouchers; remaining debt reflects settlements.';
      panel.appendChild(note);
    }
    panel.hidden = false;
  }
  initializeDates();
  return { begin, current, render, url, addDates, thisMonth, sum };
})();
