function setTableHeader() {
  const thead = $("tableHead");
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>Date</th><th>Customer Name</th><th>Invoice total</th><th>Paid on invoice</th><th>Debt Amount</th><th>Products</th><th style="text-align:right;">Actions</th></tr>`;
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

  if (!tbody) return;
  const listRequest = TransactionList.begin();
  if (listRequest === null) return;

  setMsg("pageMsg", "Loading...", false);

  const params = new URLSearchParams();

  if (searchText && searchText.trim().length > 0) {
    params.append("search", searchText.trim());
  }

  if (fromDate) {
    params.append("fromDate", fromDate);
  }

  if (toDate) {
    params.append("toDate", toDate);
  }

  if (typeof RecordFilters !== 'undefined') RecordFilters.append(params);
  const url = params.toString()
    ? `${API}?${params.toString()}`
    : API;

  let list = [];
  try {
    const res = await fetch(url, { headers: { Accept: "application/json" } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    list = Array.isArray(data) ? data : [];
    if (!TransactionList.current(listRequest)) return;
    TransactionList.render('SalesInvoices', list);
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
    cells.push(escapeHtml((x.customerName ?? x.CustomerName ?? x.customer?.name ?? x.Customer?.Name ?? '-')));
    cells.push(escapeHtml(x.invoiceTotal ?? x.InvoiceTotal ?? 0));
    cells.push(escapeHtml(x.amountPaid ?? x.AmountPaid ?? 0));
    cells.push(escapeHtml((x.debtAmount ?? x.DebtAmount ?? x.debt?.amount ?? x.Debt?.Amount ?? 0)));
    const productNames =
      x.productNames ??
      x.ProductNames ??
      x.products?.map(p => p.productName ?? p.ProductName ?? p.name ?? p.Name).filter(Boolean) ??
      [];

    cells.push(
      escapeHtml(
        Array.isArray(productNames) && productNames.length > 0
          ? productNames.join(', ')
          : '-'
      )
    );

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