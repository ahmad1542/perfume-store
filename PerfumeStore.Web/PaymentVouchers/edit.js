// edit.js

async function initPage() {
  try {
    await fillPeopleSelect('SupplierPhone', window.API_ENDPOINTS.suppliers);
    await fillMoneyAccounts('MoneyAccountID');
  } catch (e) { console.error(e); }
}


async function fillPeopleSelect(selectId, apiUrl) {
  const el = $(selectId);
  if (!el) return;
  el.innerHTML = `<option value="">-- Select --</option>`;
  let list = [];
  try { list = await apiGetJson(apiUrl); } catch { list = []; }
  (Array.isArray(list)?list:[]).forEach(p => {
    const phone = p.phone ?? p.Phone ?? "";
    const name = p.name ?? p.Name ?? "";
    if (!phone) return;
    const opt = document.createElement("option");
    opt.value = phone;
    opt.textContent = name ? `${name} (${phone})` : phone;
    el.appendChild(opt);
  });
}

async function fillMoneyAccounts(selectId) {
  const el = $(selectId);
  if (!el) return;
  el.innerHTML = `<option value="">-- Select --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.moneyAccounts); } catch { list = []; }
  (Array.isArray(list)?list:[]).forEach(a => {
    const id = a.id ?? a.ID ?? a.Id ?? "";
    const name = a.accountName ?? a.AccountName ?? ("Account #" + id);
    if (id === "") return;
    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name;
    el.appendChild(opt);
  });
}

async function fillProductsToElement(selectEl) {
  if (!selectEl) return;
  selectEl.innerHTML = `<option value="">-- Select product --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.products); } catch { list = []; }
  (Array.isArray(list)?list:[]).forEach(p => {
    const id = p.id ?? p.ID ?? "";
    const name = p.name ?? p.Name ?? ("Product #" + id);
    if (id === "") return;
    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name;
    selectEl.appendChild(opt);
  });
}

function wireDebtToggle() {
  const cb = $("HasDebt");
  const amount = $("DebtAmount");
  const notes = $("DebtNotes");
  function apply() {
    const on = !!cb?.checked;
    if (amount) amount.disabled = !on;
    if (notes) notes.disabled = !on;
  }
  if (cb) cb.addEventListener("change", apply);
  apply();
}

function setupInvoiceItems() {
  window._items = [];
  const btn = $("btnAddItem");
  if (btn) btn.addEventListener("click", () => addItemRow());
  addItemRow();
}

function addItemRow() {
  const rows = $("itemsRows");
  if (!rows) return;
  const idx = rows.querySelectorAll('tr').length + 1;
  const tr = document.createElement('tr');
  tr.innerHTML = `
    <td>${idx}</td>
    <td><select class="search-input" data-role="product"></select></td>
    <td><input class="search-input" type="number" min="1" value="1" data-role="qty" /></td>
    <td style="text-align:right;">
      <button type="button" class="btn-light" data-role="remove">Remove</button>
    </td>
  `;
  rows.appendChild(tr);

  const productSel = tr.querySelector('select[data-role="product"]');
  fillProductsToElement(productSel);

  tr.querySelector('[data-role="remove"]').addEventListener('click', () => {
    tr.remove();
    rebuildItemsFromUI();
  });

  tr.querySelector('[data-role="qty"]').addEventListener('input', rebuildItemsFromUI);
  productSel.addEventListener('change', rebuildItemsFromUI);

  rebuildItemsFromUI();
}

function rebuildItemsFromUI() {
  const rows = $("itemsRows");
  if (!rows) return;
  const items = [];
  rows.querySelectorAll('tr').forEach(tr => {
    const pid = tr.querySelector('select[data-role="product"]')?.value || "";
    const qty = Number(tr.querySelector('input[data-role="qty"]')?.value || 0);
    if (pid && qty > 0) items.push({ productId: Number(pid), qty });
  });
  window._items = items;
  const msg = $("itemsMsg");
  if (msg) msg.textContent = items.length ? `${items.length} item(s) ready.` : "No items selected yet.";
}


async function initEdit() {
  const id = getQueryParam('id');
  if (!id) {
    setMsg('pageMsg', 'Missing id in URL.', true);
    return;
  }

  if (typeof initPage === 'function') await initPage();

  setMsg('pageMsg', 'Loading...', false);

  try {
    const item = await apiGetJson(`${API}/${encodeURIComponent(id)}`);
    if ($('Date')) $('Date').value = item.Date ?? item.date ?? item.Date ?? '';
    if ($('SupplierPhone')) $('SupplierPhone').value = item.SupplierPhone ?? item.supplierphone ?? item.Supplierphone ?? '';
    if ($('PurchaseInvoiceID')) $('PurchaseInvoiceID').value = item.PurchaseInvoiceID ?? item.purchaseinvoiceid ?? item.Purchaseinvoiceid ?? '';
    if ($('MoneyAccountID')) $('MoneyAccountID').value = item.MoneyAccountID ?? item.moneyaccountid ?? item.Moneyaccountid ?? '';
    if ($('Amount')) $('Amount').value = item.Amount ?? item.amount ?? item.Amount ?? '';
    if ($('Notes')) $('Notes').value = item.Notes ?? item.notes ?? item.Notes ?? '';

    setMsg('pageMsg', '');
  } catch (e) {
    setMsg('pageMsg', 'Failed to load record from API.', true);
    console.error(e);
    return;
  }

  const btn = $('btnSave');
  if (btn) btn.addEventListener('click', () => updateItem(id));
}

function buildBodyFromForm() {
  const body = {};
  body['Date'] = $('Date').value || null;
  body['SupplierPhone'] = $('SupplierPhone').value || null;
  body['PurchaseInvoiceID'] = $('PurchaseInvoiceID').value || null;
  body['MoneyAccountID'] = $('MoneyAccountID').value || null;
  body['Amount'] = Number($('Amount').value || 0);
  body['Notes'] = $('Notes').value || null;
  return body;
}

async function updateItem(id) {
  setMsg('pageMsg', 'Saving...', false);
  try {
    const body = buildBodyFromForm();
    await apiSendJson(`${API}/${encodeURIComponent(id)}`, 'PATCH', body);
    window.location.href = 'index.html';
  } catch (e) {
    setMsg('pageMsg', 'Update failed. Check API and payload.', true);
    console.error(e);
  }
}
