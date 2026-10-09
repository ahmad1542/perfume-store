async function initPage() {
  try {
    await fillPeopleSelect("SupplierId", window.API_ENDPOINTS.suppliers);
    await fillMoneyAccounts("MoneyAccountId");
    setupInvoiceItems();
    wireDebtAndPaymentRules();
  } catch (e) {
    console.error(e);
  }
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

  (Array.isArray(list) ? list : []).forEach((p) => {
    const id = p.id ?? p.ID ?? p.Id ?? "";
    const phone = p.phone ?? p.Phone ?? "";
    const name = p.name ?? p.Name ?? "";

    if (!id) return;

    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name
      ? `${name}${phone ? ` (${phone})` : ""}`
      : `Supplier #${id}`;

    el.appendChild(opt);
  });
}

async function fillMoneyAccounts(selectId) {
  const el = $(selectId);
  if (!el) return;

  el.innerHTML = `<option value="">-- Select --</option>`;

  let list = [];
  try {
    list = await apiGetJson(window.API_ENDPOINTS.moneyAccounts);
  } catch {
    list = [];
  }

  (Array.isArray(list) ? list : []).forEach((a) => {
    const id = a.id ?? a.ID ?? a.Id ?? "";
    const name = a.accountName ?? a.AccountName ?? a.name ?? a.Name ?? ("Account #" + id);

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
  try {
    list = await apiGetJson(window.API_ENDPOINTS.products);
  } catch {
    list = [];
  }

  (Array.isArray(list) ? list : []).forEach((p) => {
    const id = p.id ?? p.ID ?? p.Id ?? "";
    const name = p.name ?? p.Name ?? ("Product #" + id);

    if (id === "") return;

    const opt = document.createElement("option");
    opt.value = id;
    opt.dataset.unitPrice = String(p.costPrice ?? p.CostPrice ?? 0);
    opt.textContent = name;
    selectEl.appendChild(opt);
  });
}

function wireDebtAndPaymentRules() {
  const amountPaidInput = $("AmountPaid");
  const moneyAccountSelect = $("MoneyAccountId");
  const hasDebtCheckbox = $("HasDebt");
  const debtGroup = $("debt-group");
  const debtAmountInput = $("DebtAmount");
  const debtNotesInput = $("DebtNotes");

  function applyRules() {
    const amountPaid = Number(amountPaidInput?.value || 0);
    const hasDebt = !!hasDebtCheckbox?.checked;

    if (moneyAccountSelect) {
      if (amountPaid > 0) {
        moneyAccountSelect.disabled = false;
        moneyAccountSelect.required = true;
      } else {
        moneyAccountSelect.disabled = true;
        moneyAccountSelect.required = false;
        moneyAccountSelect.value = "";
      }
    }

    if (amountPaid <= 0) {
      if (hasDebtCheckbox) {
        hasDebtCheckbox.checked = true;
        hasDebtCheckbox.disabled = true;
      }

      if (debtGroup) debtGroup.style.display = "block";
      if (debtAmountInput) {
        debtAmountInput.disabled = false;
        debtAmountInput.required = true;
      }
      if (debtNotesInput) {
        debtNotesInput.disabled = false;
      }

      return;
    }

    if (hasDebtCheckbox) {
      hasDebtCheckbox.disabled = false;
    }

    if (hasDebt) {
      if (debtGroup) debtGroup.style.display = "block";
      if (debtAmountInput) {
        debtAmountInput.disabled = false;
        debtAmountInput.required = true;
      }
      if (debtNotesInput) {
        debtNotesInput.disabled = false;
      }
    } else {
      if (debtGroup) debtGroup.style.display = "none";
      if (debtAmountInput) {
        debtAmountInput.disabled = true;
        debtAmountInput.required = false;
        debtAmountInput.value = "";
      }
      if (debtNotesInput) {
        debtNotesInput.disabled = true;
        debtNotesInput.value = "";
      }
    }
  }

  if (amountPaidInput) {
    amountPaidInput.addEventListener("input", applyRules);
  }

  if (hasDebtCheckbox) {
    hasDebtCheckbox.addEventListener("change", applyRules);
  }

  applyRules();
}

function setupInvoiceItems() {
  window._items = [];

  const btn = $("btnAddItem");
  if (btn) {
    btn.addEventListener("click", () => addItemRow());
  }
}

async function loadExistingItems(items) {
  const rows = $("itemsRows");
  if (!rows) return;

  rows.innerHTML = "";

  const list = Array.isArray(items) ? items : [];

  if (!list.length) {
    addItemRow();
    rebuildItemsFromUI();
    return;
  }

  for (const item of list) {
    await addItemRow({
      productId:
        item.productId ??
        item.ProductId ??
        item.ProductID ??
        item.productID ??
        "",
      qty:
        item.quantity ??
        item.Quantity ??
        item.qty ??
        1,
      unitPrice: item.unitCost ?? item.UnitCost ?? null
    });
  }

  rebuildItemsFromUI();
}

async function addItemRow(initialData = null) {
  const initialDataForPrice = initialData?.unitPrice;
  const rows = $("itemsRows");
  if (!rows) return;

  const tr = document.createElement("tr");
  tr.innerHTML = `
    <td></td>
    <td><select class="search-input" data-role="product"></select></td>
    <td><input class="search-input" type="number" min="1" value="1" data-role="qty" /></td>
    <td><input class="search-input" type="number" min="0" step="0.01" data-role="price" aria-label="Unit cost" placeholder="Unit cost" /></td>
    <td style="text-align:right;">
      <button type="button" class="btn-light" data-role="remove">Remove</button>
    </td>
  `;

  rows.appendChild(tr);

  const productSel = tr.querySelector('select[data-role="product"]');
  const qtyInput = tr.querySelector('input[data-role="qty"]');

  await fillProductsToElement(productSel);

  if (initialData) {
    if (productSel && initialData.productId) {
      productSel.value = String(initialData.productId);
    }

    if (qtyInput) {
      qtyInput.value = String(Number(initialData.qty || 1));
    }
  }

  tr.querySelector('[data-role="remove"]').addEventListener("click", () => {
    tr.remove();
    rebuildRowNumbers();
    rebuildItemsFromUI();

    const rowsCount = $("itemsRows")?.querySelectorAll("tr").length || 0;
    if (rowsCount === 0) {
      addItemRow();
    }
  });

  qtyInput.addEventListener("input", rebuildItemsFromUI);
  const priceInput = tr.querySelector('[data-role="price"]');
  if (initialDataForPrice !== undefined && initialDataForPrice !== null) {
    priceInput.value = String(initialDataForPrice);
  }
  priceInput.addEventListener('input', rebuildItemsFromUI);
  productSel.addEventListener('change', () => {
    priceInput.value = productSel.selectedOptions[0]?.dataset.unitPrice ?? '';
    rebuildItemsFromUI();
  });

  rebuildRowNumbers();
  rebuildItemsFromUI();
}

function rebuildRowNumbers() {
  const rows = $("itemsRows");
  if (!rows) return;

  rows.querySelectorAll("tr").forEach((tr, index) => {
    const firstCell = tr.querySelector("td");
    if (firstCell) firstCell.textContent = String(index + 1);
  });
}

function rebuildItemsFromUI() {
  const rows = $("itemsRows");
  if (!rows) return;

  const items = [];

  rows.querySelectorAll("tr").forEach((tr) => {
    const pid = tr.querySelector('select[data-role="product"]')?.value || "";
    const qty = Number(tr.querySelector('input[data-role="qty"]')?.value || 0);
    const priceValue = tr.querySelector('[data-role="price"]')?.value ?? '';
    const unitPrice = priceValue === '' ? null : Number(priceValue);

    if (pid && qty > 0) {
      items.push({
        productId: Number(pid),
        qty: qty,
        unitPrice
      });
    }
  });

  window._items = items;

  const msg = $("itemsMsg");
  if (msg) {
    msg.textContent = items.length
      ? `${items.length} item(s) ready.`
      : "No items selected yet.";
  }
  if (typeof updateInvoiceBalance === "function") updateInvoiceBalance();
}

async function initEdit() {
  const id = getQueryParam("id");
  if (!id) {
    setMsg("pageMsg", "Missing id in URL.", true);
    return;
  }

  if (typeof initPage === "function") {
    await initPage();
  }

  setMsg("pageMsg", "Loading...", false);

  try {
    const item = await apiGetJson(`${API}/${encodeURIComponent(id)}`);

    if ($("Date")) {
      const rawDate = item.Date ?? item.date ?? "";
      $("Date").value = rawDate ? String(rawDate).split("T")[0] : "";
    }

    if ($("SupplierId")) {
      $("SupplierId").value = String(
        item.SupplierId ??
        item.supplierId ??
        item.SupplierID ??
        ""
      );
    }

    if ($("AmountPaid")) {
      $("AmountPaid").value = item.AmountPaid ?? item.amountPaid ?? 0;
    }

    if ($("MoneyAccountId")) {
      $("MoneyAccountId").value = String(
        item.MoneyAccountId ??
        item.moneyAccountId ??
        item.MoneyAccountID ??
        ""
      );
    }

    window._appliedVoucherPayments = Number(item.appliedVoucherPayments ?? item.AppliedVoucherPayments ?? 0);
    const debtAmountValue = item.originalDebtAmount ?? item.OriginalDebtAmount ?? item.DebtAmount ?? item.debtAmount ?? "";
    const hasDebtValue =
      item.HasDebt ??
      item.hasDebt ??
      (debtAmountValue !== null && debtAmountValue !== undefined && Number(debtAmountValue) > 0);

    if ($("HasDebt")) {
      $("HasDebt").checked = !!hasDebtValue;
    }

    if ($("DebtAmount")) {
      $("DebtAmount").value = debtAmountValue;
    }

    if ($("DebtNotes")) {
      $("DebtNotes").value = item.DebtNotes ?? item.debtNotes ?? "";
    }

    if ($("Notes")) {
      $("Notes").value = item.Notes ?? item.notes ?? "";
    }

    if (typeof wireDebtAndPaymentRules === "function") {
      wireDebtAndPaymentRules();
    }

    await loadExistingItems(
      item.Products ??
      item.products ??
      item.Items ??
      item.items ??
      []
    );

    setMsg("pageMsg", "");
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      "pageMsg",
      validationMsg ?? "Failed to load record from API. Check console for details.",
      true
    );
    console.error(e);
    return;
  }

  const btn = $("btnSave");
  if (btn) {
    btn.addEventListener("click", () => updateItem(id));
  }
}

function buildBodyFromForm() {
  const amountPaid = Number($("AmountPaid")?.value || 0);
  const hasDebt = !!$("HasDebt")?.checked;

  const body = {};
  body["Date"] = $("Date")?.value || null;
  body["SupplierId"] = $("SupplierId")?.value || null;
  body["AmountPaid"] = amountPaid;
  body["MoneyAccountId"] = amountPaid > 0
    ? (Number($("MoneyAccountId")?.value || 0) || null)
    : null;
  body["HasDebt"] = hasDebt || amountPaid <= 0;
  body["DebtAmount"] = (hasDebt || amountPaid <= 0)
    ? (Number($("DebtAmount")?.value || 0) || 0)
    : 0;
  body["DebtNotes"] = (hasDebt || amountPaid <= 0)
    ? ($("DebtNotes")?.value || null)
    : null;
  body["Notes"] = $("Notes")?.value || null;
  body['Products'] = Object.fromEntries(
    (window._items || []).map(x => [x.productId, x.qty])
  );

  body['UnitCosts'] = Object.fromEntries(
    (window._items || []).filter(x => x.unitPrice !== null).map(x => [x.productId, x.unitPrice])
  );
  return body;
}

async function updateItem(id) {
  setMsg("pageMsg", "Saving...", false);

  try {
    const body = buildBodyFromForm();
    const balanceError = invoiceSettlementMessage(body, true);
    if (balanceError) { setMsg("pageMsg", balanceError, true); return; }
    await apiSendJson(`${API}/${encodeURIComponent(id)}`, "PATCH", body);
    window.location.href = Number(window._appliedVoucherPayments || 0) > Number(body.DebtAmount || 0)
      ? 'view.html?id=' + encodeURIComponent(id) : 'index.html';
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      "pageMsg",
      validationMsg ?? "Update failed. Please check your input and try again.",
      true
    );
    console.error(e);
  }
}