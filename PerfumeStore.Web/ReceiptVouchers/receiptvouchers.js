function setTableHeader() {
  const thead = $('tableHead');
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>Date</th><th>Party</th><th>Type</th><th>Amount</th><th>Applied Count</th><th>Money Account</th><th style="text-align:right;">Actions</th></tr>`;
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

  const params = TransactionList.addDates(new URLSearchParams());
  if (searchText && searchText.trim().length > 0) params.append('search', searchText.trim());
  if (typeof RecordFilters !== 'undefined') RecordFilters.append(params);

  try {
    const list = await apiGetJson(params.toString() ? `${API}?${params.toString()}` : API);
    const items = Array.isArray(list) ? list : [];
    if (!TransactionList.current(listRequest)) return;
    TransactionList.render('ReceiptVouchers', items);

    tbody.innerHTML = '';
    if (countChip) countChip.textContent = items.length;

    if (items.length === 0) {
      if (emptyState) emptyState.style.display = '';
      if (tableCard) tableCard.style.display = 'none';
      setMsg('pageMsg', '', false);
      return;
    }

    if (emptyState) emptyState.style.display = 'none';
    if (tableCard) tableCard.style.display = '';

    items.forEach(item => {
      const id = item.id ?? item.ID ?? '';
      const partyName = item.partyName ?? item.PartyName ?? '-';
      const type = item.receiptForType ?? item.ReceiptForType ?? 'customer';
      const amount = Number(item.amount ?? item.Amount ?? 0).toFixed(2);
      const appliedCount = Number(item.appliedInvoicesCount ?? item.AppliedInvoicesCount ?? 0) + Number(item.appliedDebtsCount ?? item.AppliedDebtsCount ?? 0);
      const moneyAccountName = item.moneyAccountName ?? item.MoneyAccountName ?? '-';
      const notes = item.notes ?? item.Notes ?? '';
      const date = formatDateOnly(item.date ?? item.Date ?? '');

      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>#${escapeHtml(id)}</td>
        <td>${escapeHtml(date)}</td>
        <td>${escapeHtml(partyName)}</td>
        <td>${escapeHtml(type === 'person' ? 'Person Debt' : 'Customer')}</td>
        <td>${escapeHtml(amount)}</td>
        <td>${escapeHtml(String(appliedCount))}</td>
        <td>${escapeHtml(moneyAccountName)}</td>
        <td style="text-align:right; white-space:nowrap;">
          <a class="btn-light" href="view.html?id=${encodeURIComponent(id)}">View</a>
        </td>
      `;
      tr.title = notes || '';
      tbody.appendChild(tr);
    });

    setMsg('pageMsg', '', false);
  } catch (e) {
    if (!TransactionList.current(listRequest)) return;
    tbody.innerHTML = '';
    if (countChip) countChip.textContent = '0';
    if (emptyState) emptyState.style.display = 'none';
    if (tableCard) tableCard.style.display = 'none';
    setMsg('pageMsg', getFriendlyMessage(e), true);
  }
}
