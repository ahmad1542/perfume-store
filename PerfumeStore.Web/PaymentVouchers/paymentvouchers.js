function setTableHeader() {
  const thead = $('tableHead');
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>Date</th><th>Supplier</th><th>Amount</th><th>Applied Invoices</th><th>Money Account</th><th>Notes</th><th style="text-align:right;">Actions</th></tr>`;
}

async function loadList(searchText = '') {
  if (typeof RecordFilters !== 'undefined') {
    try { await RecordFilters.ready; RecordFilters.restoreLegacy(); }
    catch (error) { setMsg('pageMsg', error.message, true); return; }
    searchText = $('searchInput')?.value ?? searchText;
  }
  setTableHeader();

  const tbody = $('rows');
  const emptyState = $('emptyState');
  const tableCard = $('tableCard');
  const countChip = $('countChip');

  if (!tbody) return;
  const listRequest = TransactionList.begin();
  if (listRequest === null) return;
  setMsg('pageMsg', 'Loading...', false);

  const url = TransactionList.url(API, searchText);

  let list = [];
  try {
    list = await apiGetJson(url);
    list = Array.isArray(list) ? list : [];
    if (!TransactionList.current(listRequest)) return;
    TransactionList.render('PaymentVouchers', list);
  } catch (e) {
    if (!TransactionList.current(listRequest)) return;
    setMsg('pageMsg', 'Failed to load payment vouchers.', true);
    tbody.innerHTML = '';
    if (countChip) countChip.textContent = '0';
    if (emptyState) emptyState.style.display = '';
    if (tableCard) tableCard.style.display = 'none';
    return;
  }

  tbody.innerHTML = '';
  if (countChip) countChip.textContent = String(list.length);

  if (list.length === 0) {
    setMsg('pageMsg', '');
    if (emptyState) emptyState.style.display = '';
    if (tableCard) tableCard.style.display = 'none';
    return;
  }

  if (emptyState) emptyState.style.display = 'none';
  if (tableCard) tableCard.style.display = '';
  setMsg('pageMsg', `Loaded ${list.length} record(s).`, false);

  list.forEach((x, index) => {
    const id = x.id ?? x.ID ?? '';
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${index + 1}</td>
      <td>${escapeHtml(formatDateOnly(x.date ?? x.Date ?? ''))}</td>
      <td>${escapeHtml(x.supplierName ?? x.SupplierName ?? '-')}</td>
      <td>${escapeHtml((x.amount ?? x.Amount ?? 0).toFixed ? (x.amount ?? x.Amount ?? 0).toFixed(2) : x.amount ?? x.Amount ?? 0)}</td>
      <td>${escapeHtml(String(x.appliedInvoicesCount ?? x.AppliedInvoicesCount ?? 0))}</td>
      <td>${escapeHtml(x.moneyAccountName ?? x.MoneyAccountName ?? '-')}</td>
      <td>${escapeHtml(x.notes ?? x.Notes ?? '-')}</td>
      <td class="actions-cell">
        <a class="btn-edit" href="view.html?id=${encodeURIComponent(id)}">View</a>
      </td>
    `;
    tbody.appendChild(tr);
  });
}
