const OPEN_SALES_INVOICES_API = `${API}/open-sales-invoices`;
const OPEN_PERSON_DEBTS_API = `${API}/open-person-debts`;

function toMoney(value) {
  return Number(value || 0).toFixed(2);
}

function getReceiptForType() {
  return $('ReceiptForType')?.value || 'customer';
}

function updatePartyLabels() {
  const type = getReceiptForType();
  const customerWrap = $('customerGroup');
  const personWrap = $('personGroup');
  const allocatorTitle = $('allocatorTitle');
  const allocatorHint = $('allocatorHint');
  const invoiceEmpty = $('invoiceEmpty');
  const autoBtn = $('btnAutoAllocate');

  if (customerWrap) customerWrap.style.display = type === 'customer' ? '' : 'none';
  if (personWrap) personWrap.style.display = type === 'person' ? '' : 'none';

  if (allocatorTitle) allocatorTitle.textContent = type === 'customer' ? 'Apply Receipt to Sales Invoices' : 'Apply Receipt to Person / Supplier Debts';
  if (allocatorHint) allocatorHint.textContent = type === 'customer'
    ? 'Only unpaid sales invoices for the selected customer are shown.'
    : 'Only unpaid direct person or supplier debts are shown.';
  if (invoiceEmpty) invoiceEmpty.textContent = type === 'customer'
    ? 'Choose a customer to load open sales invoices.'
    : 'Choose a person or supplier to load open direct debts.';
  if (autoBtn) autoBtn.textContent = type === 'customer' ? 'Auto Allocate Oldest First' : 'Auto Allocate Oldest Debt First';
}

async function fillCustomers() {
  const el = $('CustomerId');
  if (!el) return;

  el.innerHTML = `<option value="">-- Select Customer --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.customers); } catch { list = []; }

  (Array.isArray(list) ? list : []).forEach(c => {
    const id = c.id ?? c.ID ?? '';
    const name = c.name ?? c.Name ?? '';
    const phone = c.phone ?? c.Phone ?? '';
    if (!id) return;

    const opt = document.createElement('option');
    opt.value = id;
    opt.textContent = name ? (phone ? `${name} (${phone})` : name) : id;
    el.appendChild(opt);
  });
}

async function fillDebtParties() {
  const el = $('PersonId');
  if (!el) return;

  el.innerHTML = `<option value="">-- Select Person / Supplier --</option>`;
  let persons = [];
  let suppliers = [];
  try { persons = await apiGetJson(window.API_ENDPOINTS.persons); } catch { persons = []; }
  try { suppliers = await apiGetJson(window.API_ENDPOINTS.suppliers); } catch { suppliers = []; }

  const seen = new Set();
  const items = [];
  (Array.isArray(persons) ? persons : []).forEach(p => items.push({ ...p, _partyType: 'Person' }));
  (Array.isArray(suppliers) ? suppliers : []).forEach(s => items.push({ ...s, _partyType: 'Supplier' }));

  items.forEach(p => {
    const id = p.id ?? p.ID ?? '';
    const name = p.name ?? p.Name ?? '';
    const phone = p.phone ?? p.Phone ?? '';
    const type = p._partyType || 'Person';
    if (!id || seen.has(id)) return;
    seen.add(id);

    const opt = document.createElement('option');
    opt.value = id;
    opt.textContent = `${type} - ${name ? (phone ? `${name} (${phone})` : name) : id}`;
    el.appendChild(opt);
  });
}

async function fillMoneyAccounts() {
  const el = $('MoneyAccountID');
  if (!el) return;

  el.innerHTML = `<option value="">-- Select Account --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.moneyAccounts); } catch { list = []; }

  (Array.isArray(list) ? list : []).forEach(a => {
    const id = a.id ?? a.ID ?? '';
    const name = a.accountName ?? a.AccountName ?? `Account #${id}`;
    if (id === '') return;

    const opt = document.createElement('option');
    opt.value = id;
    opt.textContent = name;
    el.appendChild(opt);
  });
  selectSafeMoneyAccount(el);
}

async function loadOpenItems() {
  const type = getReceiptForType();
  const partyId = type === 'customer' ? ($('CustomerId')?.value || '') : ($('PersonId')?.value || '');
  const empty = $('invoiceEmpty');
  const card = $('invoiceTableCard');
  const rows = $('invoiceRows');

  rows.innerHTML = '';

  if (!partyId) {
    if (empty) {
      empty.style.display = '';
      empty.textContent = type === 'customer'
        ? 'Choose a customer to load open sales invoices.'
        : 'Choose a person or supplier to load open direct debts.';
    }
    if (card) card.style.display = 'none';
    updateSummary();
    return;
  }

  let list = [];
  try {
    list = await apiGetJson(`${type === 'customer' ? OPEN_SALES_INVOICES_API : OPEN_PERSON_DEBTS_API}/${encodeURIComponent(partyId)}`);
  } catch (e) {
    if (empty) {
      empty.style.display = '';
      empty.textContent = type === 'customer' ? 'Failed to load open sales invoices.' : 'Failed to load open person or supplier debts.';
    }
    if (card) card.style.display = 'none';
    return;
  }

  if (!Array.isArray(list) || list.length === 0) {
    if (empty) {
      empty.style.display = '';
      empty.textContent = type === 'customer'
        ? 'This customer has no unpaid sales invoices.'
        : 'This person or supplier has no unpaid direct debts.';
    }
    if (card) card.style.display = 'none';
    updateSummary();
    return;
  }

  list.forEach(x => {
    const itemId = type === 'customer'
      ? (x.salesInvoiceId ?? x.SalesInvoiceId)
      : (x.debtId ?? x.DebtId);
    const remainingAmount = Number(x.remainingAmount ?? x.RemainingAmount ?? 0);
    const notes = x.notes ?? x.Notes ?? '';
    const itemDate = formatDateOnly(type === 'customer'
      ? (x.invoiceDate ?? x.InvoiceDate ?? '')
      : (x.date ?? x.Date ?? ''));

    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${type === 'customer' ? '#' + itemId : 'Debt #' + itemId}</td>
      <td>${escapeHtml(itemDate)}</td>
      <td>${escapeHtml(notes || '-')}</td>
      <td>${escapeHtml(toMoney(remainingAmount))}</td>
      <td>
        <input
          type="number"
          class="search-input allocation-input"
          min="0"
          max="${remainingAmount}"
          step="0.01"
          value="0"
          data-item-type="${type}"
          data-item-id="${itemId}"
          data-remaining="${remainingAmount}"
        />
      </td>
    `;
    rows.appendChild(tr);
  });

  if (empty) empty.style.display = 'none';
  if (card) card.style.display = '';

  rows.querySelectorAll('.allocation-input').forEach(input => {
    input.addEventListener('input', () => {
      const max = Number(input.dataset.remaining || 0);
      let value = Number(input.value || 0);
      if (value < 0) value = 0;
      if (value > max) value = max;
      input.value = value;
      updateSummary();
    });
  });

  updateSummary();
}

function getApplications() {
  const inputs = document.querySelectorAll('.allocation-input');
  const salesApplications = [];
  const personDebtApplications = [];

  inputs.forEach(input => {
    const appliedAmount = Number(input.value || 0);
    if (appliedAmount <= 0) return;

    if ((input.dataset.itemType || 'customer') === 'customer') {
      salesApplications.push({
        salesInvoiceId: Number(input.dataset.itemId),
        appliedAmount,
      });
    } else {
      personDebtApplications.push({
        debtId: Number(input.dataset.itemId),
        appliedAmount,
      });
    }
  });

  return { salesApplications, personDebtApplications };
}

function updateSummary() {
  const voucherAmount = Number($('Amount')?.value || 0);
  const applications = getApplications();
  const appliedAmount = [...applications.salesApplications, ...applications.personDebtApplications]
    .reduce((sum, x) => sum + Number(x.appliedAmount || 0), 0);
  const difference = voucherAmount - appliedAmount;

  if ($('summaryVoucherAmount')) $('summaryVoucherAmount').textContent = toMoney(voucherAmount);
  if ($('summaryAppliedAmount')) $('summaryAppliedAmount').textContent = toMoney(appliedAmount);

  const diffEl = $('summaryDifference');
  if (diffEl) {
    diffEl.textContent = toMoney(difference);
    if (Math.abs(difference) < 0.01) diffEl.style.color = '#22c55e';
    else if (difference > 0) diffEl.style.color = '#f59e0b';
    else diffEl.style.color = '#ef4444';
  }
}

function autoAllocateOldestFirst() {
  const target = Number($('Amount')?.value || 0);
  let remaining = target;

  document.querySelectorAll('.allocation-input').forEach(input => {
    const max = Number(input.dataset.remaining || 0);
    const applied = remaining <= 0 ? 0 : Math.min(max, remaining);
    input.value = applied.toFixed(2);
    remaining -= applied;
  });

  updateSummary();
}

function buildBodyFromForm() {
  const receiptForType = getReceiptForType();
  const applications = getApplications();

  return {
    date: $('Date').value || null,
    receiptForType,
    customerId: receiptForType === 'customer' ? ($('CustomerId').value || null) : null,
    personId: receiptForType === 'person' ? ($('PersonId').value || null) : null,
    moneyAccountID: Number($('MoneyAccountID').value || 0),
    debtId: receiptForType === 'person' ? (applications.personDebtApplications[0]?.debtId ?? null) : null,
    amount: Number($('Amount').value || 0),
    notes: $('Notes').value || null,
    salesApplications: applications.salesApplications,
    personDebtApplications: applications.personDebtApplications,
  };
}

async function createItem() {
  setMsg('pageMsg', 'Saving...', false);
  try {
    const body = buildBodyFromForm();
    const voucherAmount = Number(body.amount || 0);
    const appliedAmount = [...(body.salesApplications || []), ...(body.personDebtApplications || [])]
      .reduce((sum, x) => sum + Number(x.appliedAmount || 0), 0);
    const difference = voucherAmount - appliedAmount;

    if (voucherAmount <= 0) {
      setMsg('pageMsg', 'Voucher amount must be greater than 0', true);
      return;
    }

    if (!body.moneyAccountID) {
      setMsg('pageMsg', 'Money account is required', true);
      return;
    }

    if (body.receiptForType === 'customer' && !body.customerId) {
      setMsg('pageMsg', 'Customer is required', true);
      return;
    }

    if (body.receiptForType === 'person' && !body.personId) {
      setMsg('pageMsg', 'Person or supplier is required', true);
      return;
    }

    if (body.receiptForType === 'person' && (body.personDebtApplications || []).length > 1) {
      setMsg('pageMsg', 'Only one debt can be selected for person receipt voucher', true);
      return;
    }

    if ((body.salesApplications || []).length === 0 && (body.personDebtApplications || []).length === 0) {
      setMsg('pageMsg', 'You must allocate at least one item', true);
      return;
    }

    if (Math.abs(difference) > 0.01) {
      setMsg('pageMsg', 'Voucher amount must equal total applied amount', true);
      return;
    }

    await apiSendJson(API, 'POST', body);
    window.location.href = 'index.html';
  } catch (e) {
    setMsg('pageMsg', getFriendlyMessage(e), true);
    console.error(e);
  }
}

async function initPage() {
  $('Date').value = new Date().toISOString().split('T')[0];
  await fillCustomers();
  await fillDebtParties();
  await fillMoneyAccounts();
  updatePartyLabels();
  updateSummary();

  $('ReceiptForType')?.addEventListener('change', () => {
    updatePartyLabels();
    loadOpenItems();
  });
  $('CustomerId')?.addEventListener('change', loadOpenItems);
  $('PersonId')?.addEventListener('change', loadOpenItems);
  $('Amount')?.addEventListener('input', updateSummary);
  $('btnAutoAllocate')?.addEventListener('click', autoAllocateOldestFirst);
}
