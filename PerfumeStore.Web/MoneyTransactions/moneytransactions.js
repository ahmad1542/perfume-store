function setTableHeader() {
  const thead = $("tableHead");
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>Date</th><th>From</th><th>To</th><th>TransferAmount</th><th style="text-align:right;">Actions</th></tr>`;
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

  if (!tbody) return;
  const listRequest = TransactionList.begin();
  if (listRequest === null) return;

  setMsg("pageMsg", "Loading...", false);

  const url = TransactionList.url(API, searchText);

  let list = [];
  try {
    const res = await fetch(url, { headers: { Accept: "application/json" } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    list = Array.isArray(data) ? data : [];
    if (!TransactionList.current(listRequest)) return;
    TransactionList.render('MoneyTransactions', list);
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
    const cells = [];
    const rawDate = x.date ?? x.Date ?? '';
    cells.push(escapeHtml(formatDateOnly(rawDate)));
    cells.push(escapeHtml((x.fromMoneyAccountName ?? x.fromMoneyAccountID ?? x.FromMoneyAccountID ?? '-')));
    cells.push(escapeHtml((x.toMoneyAccountName ?? x.toMoneyAccountID ?? x.ToMoneyAccountID ?? '-')));
    cells.push(escapeHtml(x.transferAmount ?? x.TransferAmount ?? 0));

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(id)}</td>
      ${cells.map(c => `<td>${c}</td>`).join("")}
      <td class="actions-cell">
        <a class="btn-edit" href="view.html?id=${encodeURIComponent(id)}">View</a>
        <a class="btn-edit" href="edit.html?id=${encodeURIComponent(id)}">Edit</a>
      </td>
    `;
    tbody.appendChild(tr);
  });
}
