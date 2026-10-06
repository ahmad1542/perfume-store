window._productPrices = {};

function buildBodyFromForm() {
  const body = {};
  body['Date'] = $('Date').value || null;
  body['CustomerId'] = $('CustomerId').value || null;
  body['AmountPaid'] = Number($('AmountPaid').value || 0);
  body['MoneyAccountId'] = $('MoneyAccountId').value ? Number($('MoneyAccountId').value) : null;
  body['HasDebt'] = !!$('HasDebt').checked;
  body['DebtAmount'] = Number($('DebtAmount').value || 0);
  body['DebtNotes'] = $('DebtNotes').value || null;
  body['Notes'] = $('Notes').value || null;
  body['Products'] = Object.fromEntries(
    (window._items || []).map(x => [x.productId, x.qty])
  );
  body['UnitPrices'] = Object.fromEntries(
    (window._items || []).filter(x => x.unitPrice !== null).map(x => [x.productId, x.unitPrice])
  );
  return body;
}

async function createItem() {
  setMsg('pageMsg', 'Saving...', false);
  try {
    const body = buildBodyFromForm();

    if (body.HasDebt && Number(body.DebtAmount || 0) > 0 && !body.CustomerId) {
      setMsg(
        'pageMsg',
        'Customer must be selected when the sales invoice has debt.',
        true
      );
      $('CustomerId')?.focus();
      return;
    }

    await apiSendJson(API, 'POST', body);
    setMsg("pageMsg", "Sales invoice created successfully.");
    window.location.href = 'index.html';
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      'pageMsg',
      validationMsg ?? 'Save failed. Check API and payload.',
      true
    );
    console.error(e);
  }
}

async function fillMoneyAccounts(selectId) {
  const el = $(selectId);
  if (!el) return;
  el.innerHTML = `<option value="">-- Select Account --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.moneyAccounts); } catch { list = []; }
  (Array.isArray(list) ? list : []).forEach(a => {
    const id = a.id ?? a.ID ?? a.Id ?? "";
    const name = a.accountName ?? a.AccountName ?? ("Account #" + id);
    if (id === "") return;
    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name;
    el.appendChild(opt);
    if (name == "Safe") {
      const toggledElement = $("debt-group");
      const hasDebtCheckbox = $("HasDebt");
      if (hasDebtCheckbox) hasDebtCheckbox.checked = false;
      toggledElement.style.display = "none";
      const moneyAccountSelect = $(selectId);
      if (moneyAccountSelect) {
        moneyAccountSelect.value = id;
      }
    }
  });

}

async function initPage() {
  try {
    const today = new Date().toISOString().split('T')[0];
    if ($('Date') && !$('Date').value) $('Date').value = today;

    await fillPeopleSelect('CustomerId', window.API_ENDPOINTS.customers);
    fillMoneyAccounts('MoneyAccountId');
    setupInvoiceItems();
    wireDebtToggle();
    const amountPaidInput = $("AmountPaid");
    const moneyAccountSelect = $("MoneyAccountId");
    const hasDebtCheckbox = $("HasDebt");
    const toggledElement = $("debt-group");
    const debtAmount = $("DebtAmount");
    const debtNotes = $("DebtNotes");
    const customerId = $("CustomerId");
    const customerStar = $("customerRequiredStar");

    function applyDebtCustomerRules() {
      const hasDebt = !!hasDebtCheckbox?.checked;
      const debtValue = Number(debtAmount?.value || 0);

      const isRequired = hasDebt && debtValue > 0;

      if (customerId) {
        customerId.required = isRequired;
      }

      if (customerStar) {
        customerStar.style.display = isRequired ? "inline" : "none";
      }
    }

    function applyAmountPaidRules() {
      const amount = Number(amountPaidInput?.value || 0);

      if (!moneyAccountSelect) return;

      if (amount > 0) {
        moneyAccountSelect.disabled = false;
        moneyAccountSelect.required = true;
      } else {
        moneyAccountSelect.disabled = true;
        moneyAccountSelect.required = false;
        moneyAccountSelect.value = "";

        if (hasDebtCheckbox) {
          hasDebtCheckbox.checked = true;
        }

        if (toggledElement) {
          toggledElement.style.display = "block";
          debtAmount.disabled = false;
          debtNotes.disabled = false;
        }
        if (debtAmount) debtAmount.required = true;
        if (customerId) customerId.required = true;
      }

      applyDebtCustomerRules();
    }

    if (debtAmount) {
      debtAmount.addEventListener("input", applyDebtCustomerRules);
    }

    if (amountPaidInput) {
      amountPaidInput.addEventListener("input", applyAmountPaidRules);
    }

    if (hasDebtCheckbox) {
      hasDebtCheckbox.addEventListener("change", function () {
        const amount = Number(amountPaidInput?.value || 0);

        if (amount <= 0) {
          this.checked = true;
          if (toggledElement) toggledElement.style.display = "block";
          if (debtAmount) debtAmount.required = true;
          if (customerId) customerId.required = true;
          return;
        }

        if (this.checked) {
          if (debtAmount) debtAmount.required = true;
          if (toggledElement) toggledElement.style.display = "block";
          if (customerId) customerId.required = true;
        } else {
          if (toggledElement) toggledElement.style.display = "none";
          if (debtAmount) debtAmount.required = false;
          if (customerId) customerId.required = false;
          if (debtAmount) debtAmount.value = "";
          if (debtNotes) debtNotes.value = "";
        }
        applyDebtCustomerRules();
      });
    }

    applyAmountPaidRules();
    applyDebtCustomerRules();
  } catch (e) { console.error(e); }
}

async function fillPeopleSelect(selectId, apiUrl) {
  const el = $(selectId);
  if (!el) return;

  el.innerHTML = `<option value="">-- Select --</option>`;

  let list = [];
  try {
    list = await apiGetJson(apiUrl);
  } catch {
    list = [];
  }

  (Array.isArray(list) ? list : []).forEach(p => {
    const id = p.id ?? p.ID ?? p.customerId ?? p.CustomerId ?? "";
    const phone = p.phone ?? p.Phone ?? "";
    const name = p.name ?? p.Name ?? "";

    if (!id) return;

    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name
      ? phone ? `${name} (${phone})` : name
      : id;

    el.appendChild(opt);
  });
}

async function fillProductsToElement(selectEl) {
  if (!selectEl) return;
  selectEl.innerHTML = `<option value="">-- Select product --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.products); } catch { list = []; }
  (Array.isArray(list) ? list : []).forEach(p => {
    const id = p.id ?? p.ID ?? "";
    const name = p.name ?? p.Name ?? ("Product #" + id);
    const salePrice = Number(p.salePrice ?? p.SalePrice ?? 0);

    if (id === "") return;

    window._productPrices[id] = salePrice;

    const opt = document.createElement("option");
    opt.value = id;
    opt.dataset.unitPrice = String(p.salePrice ?? p.SalePrice ?? 0);
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

async function addItemRow() {
  const initialDataForPrice = undefined;
  const rows = $("itemsRows");
  if (!rows) return;
  const idx = rows.querySelectorAll('tr').length + 1;
  const tr = document.createElement('tr');
  tr.innerHTML = `
    <td>${idx}</td>
    <td><select class="search-input" data-role="product"></select></td>
    <td><input class="search-input" type="number" min="1" value="1" data-role="qty" /></td>
    <td><input class="search-input" type="number" min="0" step="0.01" data-role="price" aria-label="Unit sale price" placeholder="Unit sale price" /></td>
    <td style="text-align:right;">
      <button type="button" class="btn-light" data-role="remove">Remove</button>
    </td>
  `;
  rows.appendChild(tr);

  const productSel = tr.querySelector('select[data-role="product"]');
  await fillProductsToElement(productSel);

  tr.querySelector('[data-role="remove"]').addEventListener('click', () => {
    tr.remove();
    rebuildItemsFromUI();
  });

  tr.querySelector('[data-role="qty"]').addEventListener('input', rebuildItemsFromUI);
  const priceInput = tr.querySelector('[data-role="price"]');
  if (initialDataForPrice !== undefined && initialDataForPrice !== null) {
    priceInput.value = String(initialDataForPrice);
  }
  priceInput.addEventListener('input', rebuildItemsFromUI);
  productSel.addEventListener('change', () => {
    priceInput.value = productSel.selectedOptions[0]?.dataset.unitPrice ?? '';
    rebuildItemsFromUI();
  });

  rebuildItemsFromUI();
}

function rebuildItemsFromUI() {
  const rows = $("itemsRows");
  if (!rows) return;
  const items = [];
  rows.querySelectorAll('tr').forEach(tr => {
    const pid = tr.querySelector('select[data-role="product"]')?.value || "";
    const qty = Number(tr.querySelector('input[data-role="qty"]')?.value || 0);
    const priceValue = tr.querySelector('[data-role="price"]')?.value ?? '';
    const unitPrice = priceValue === '' ? null : Number(priceValue);
    if (pid && qty > 0) items.push({ productId: Number(pid), qty, unitPrice });
  });
  window._items = items;
  const msg = $("itemsMsg");
  if (msg) msg.textContent = items.length ? `${items.length} item(s) ready.` : "No items selected yet.";

  updateAmountPaidFromItems();
}

function updateAmountPaidFromItems() {
  const amountPaidInput = $("AmountPaid");
  if (!amountPaidInput) return;

  const items = window._items || [];

  const total = items.reduce((sum, item) => {
    const price = Number(item.unitPrice ?? window._productPrices?.[item.productId] ?? 0);
    const qty = Number(item.qty || 0);
    return sum + (price * qty);
  }, 0);

  amountPaidInput.value = total > 0 ? total : "";

  amountPaidInput.dispatchEvent(new Event("input", { bubbles: true }));
}