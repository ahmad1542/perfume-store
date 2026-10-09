const lowStockOnly = getQueryParam("lowStock") === "true";

function setTableHeader() {
  if (lowStockOnly) {
    $("inventoryTitle").textContent = "Low stock items";
    $("inventorySubtitle").textContent = "Items at or below their minimum stock. Stock levels are live.";
    $("stockFilterLink").textContent = "Show all inventory";
    $("stockFilterLink").href = "index.html";
    $("emptyTitle").textContent = "No low-stock items found";
    $("emptyDescription").textContent = "No items at or below their minimum match this search.";
    $("emptyCreate").hidden = true;
  }
  const thead = $("tableHead");
  if (!thead) return;
  thead.innerHTML = `<tr><th>#</th><th>Product Name</th><th>Quantity</th>${lowStockOnly ? "<th>Minimum stock</th><th>Status</th>" : ""}<th style="text-align:right;">Actions</th></tr>`;
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
    if (lowStockOnly) list = list.filter(x => x.minStock != null && Number(x.quantity) <= Number(x.minStock));
    if (lowStockOnly) {
      const outOfStock = list.filter(x => Number(x.quantity) === 0).length;
      $("lowStockSummary").textContent = `${list.length} low-stock item(s) · ${outOfStock} out of stock. Items at the minimum are included.`;
      $("lowStockSummary").hidden = false;
    }
  } catch (e) {
    setMsg("pageMsg", "Failed to load data from API.", true);
    if (lowStockOnly) $("lowStockSummary").hidden = true;
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
    const id = x.productId ?? x.productID ?? '';
    const cells = [];
    cells.push(escapeHtml((x.product?.name ?? x.Product?.Name ?? x.productName ?? x.ProductName ?? "").toString()));
    cells.push(escapeHtml(x.quantity ?? x.Quantity ?? 0));
    if (lowStockOnly) {
      cells.push(escapeHtml(x.minStock));
      const status = Number(x.quantity) === 0 ? "Out of stock" : Number(x.quantity) === Number(x.minStock) ? "At minimum" : "Below minimum";
      cells.push(`<span class="stock-status${Number(x.quantity) === 0 ? ' out' : ''}">${status}</span>`);
    }

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
