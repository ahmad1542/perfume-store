function setTableHeader() {
  const thead = $("tableHead");
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>Date</th><th>Type</th><th>Amount</th><th>Money Account</th>  <th>Notes</th><th style="text-align:right;">Actions</th></tr>`;
}

async function loadList(searchText = "") {
  if (typeof RecordFilters !== 'undefined') {
    try { await RecordFilters.ready; RecordFilters.restoreLegacy(); }
    catch (error) { setMsg('pageMsg', error.message, true); return; }
    searchText = $('searchInput')?.value ?? searchText;
  }
  setTableHeader();

  const tbody = $("rows");
  const emptyState = $("emptyState");
  const tableCard = $("tableCard");
  const countChip = $("countChip");
  const fromDate = $("fromDateInput")?.value?.trim() || "";
  const toDate = $("toDateInput")?.value?.trim() || "";
  const selectedExpenseTypeIds = typeof getSelectedExpenseTypeIds === "function" ? getSelectedExpenseTypeIds() : [];
  const totalExpenseTypeCheckboxes = document.querySelectorAll('.expense-type-filter-checkbox').length;

  if (!tbody) return;
  const listRequest = TransactionList.begin();
  if (listRequest === null) return;

  setMsg("pageMsg", "Loading...", false);

  const params = new URLSearchParams();
  if (searchText && searchText.trim()) params.append('search', searchText.trim());
  if (fromDate) params.append('fromDate', fromDate);
  if (toDate) params.append('toDate', toDate);

  if (totalExpenseTypeCheckboxes > 0 && selectedExpenseTypeIds.length === 0) {
    if (typeof RecordFilters !== 'undefined') RecordFilters.append(params);
    TransactionList.render('Expenses', []);
    setMsg("pageMsg", "No expense types are selected.", false);
    tbody.innerHTML = "";
    if (countChip) countChip.textContent = "0";
    if (emptyState) emptyState.style.display = "";
    if (tableCard) tableCard.style.display = "none";
    return;
  }

  selectedExpenseTypeIds.forEach(id => params.append('expenseTypeIds', id));

  if (typeof RecordFilters !== 'undefined') RecordFilters.append(params);
  const url = params.toString() ? `${API}?${params.toString()}` : API;

  let list = [];
  try {
    list = await apiGetJson(url);
    list = Array.isArray(list) ? list : [];
    if (!TransactionList.current(listRequest)) return;
    TransactionList.render('Expenses', list);
  } catch (e) {
    if (!TransactionList.current(listRequest)) return;
    setMsg("pageMsg", "Failed to load data from API.", true);
    tbody.innerHTML = "";
    if (countChip) countChip.textContent = "0";
    if (emptyState) emptyState.style.display = "";
    if (tableCard) tableCard.style.display = "none";
    return;
  }

  if (countChip) countChip.textContent = String(list.length);
  tbody.innerHTML = "";

  if (list.length === 0) {
    setMsg("pageMsg", "");
    if (emptyState) emptyState.style.display = "";
    if (tableCard) tableCard.style.display = "none";
    return;
  }

  if (emptyState) emptyState.style.display = "none";
  if (tableCard) tableCard.style.display = "";

  setMsg("pageMsg", `Loaded ${list.length} record(s).`);

  list.forEach((x, index) => {
    const id = x.id ?? x.ID ?? x.Id ?? '';
    const date = formatDateOnly(x.date ?? x.Date ?? '');
    const type = x.expenseTypeName ?? x.ExpenseTypeName ?? '-';
    const amount = x.amount ?? x.Amount ?? 0;
    const moneyAccount = x.moneyAccountName ?? x.MoneyAccountName ?? '-';
    const notes = x.notes ?? x.Notes ?? '';

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(index + 1)}</td>
      <td>${escapeHtml(date)}</td>
      <td>${escapeHtml(type)}</td>
      <td>${escapeHtml(amount)}</td>
      <td>${escapeHtml(moneyAccount)}</td>
      <td class="notes-cell">${escapeHtml(notes)}</td>
      <td class="actions-cell">
        <a class="btn-edit" href="view.html?id=${encodeURIComponent(id)}">View</a>
        <a class="btn-edit" href="edit.html?id=${encodeURIComponent(id)}">Edit</a>
        <button type="button" class="btn-edit btn-delete">Delete</button>
      </td>
    `;
    const deleteButton = tr.querySelector('.btn-delete');
    deleteButton.addEventListener('click', () => deleteExpense(id, amount, moneyAccount, deleteButton));
    tbody.appendChild(tr);
  });
}

async function deleteExpense(id, amount, account, button) {
  if (button.disabled) return;
  const money = '₪' + Number(amount).toFixed(2);
  if (!window.confirm(`Delete expense #${id} for ${money}?\n\nThis will restore ${money} to ${account} and remove the expense from its original month's profit calculation. The reversal will remain in history.`)) return;
  button.disabled = true;
  setMsg('pageMsg', 'Deleting expense...');
  try {
    await apiSendJson(`${API}/${encodeURIComponent(id)}`, 'DELETE');
    await loadList($('searchInput')?.value || '');
    setMsg('pageMsg', `Expense #${id} deleted. ${money} restored to ${account}.`);
  } catch (error) {
    setMsg('pageMsg', getFriendlyMessage(error), true);
    button.disabled = false;
  }
}
