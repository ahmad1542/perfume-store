function setTableHeader() {
  const thead = $("tableHead");
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>AccountName</th><th>CurrentBalance</th><th>Notes</th><th style="text-align:right;">Actions</th></tr>`;
}

async function loadList(searchText = "") {
  setTableHeader();

  const tbody = $("rows");
  const emptyState = $("emptyState");
  const tableCard = $("tableCard");
  const countChip = $("countChip");

  if (!tbody) return;

  setMsg("pageMsg", "Loading...", false);

  const url =
    searchText && searchText.trim().length > 0
      ? `${API}?search=${encodeURIComponent(searchText.trim())}`
      : API;

  let list = [];
  try {
    const res = await fetch(url, { headers: { Accept: "application/json" } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    list = Array.isArray(data) ? data : [];
  } catch (e) {
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
    cells.push(escapeHtml(x.accountName ?? x.AccountName ?? '-'));
    cells.push(escapeHtml(x.currentBalance ?? x.CurrentBalance ?? 0));
    cells.push(escapeHtml(x.notes ?? x.Notes ?? ''));

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(id)}</td>
      ${cells.map(c => `<td>${c}</td>`).join("")}
      <td class="actions-cell">
        <a class="btn-edit" href="view.html?id=${encodeURIComponent(id)}">View</a>
        <a class="btn-edit" href="../FinancialHistory/index.html?kind=cash&accountId=${encodeURIComponent(id)}">History</a>
        <a class="btn-edit" href="edit.html?id=${encodeURIComponent(id)}">Edit</a>
      </td>
    `;
    tbody.appendChild(tr);
  });
}
