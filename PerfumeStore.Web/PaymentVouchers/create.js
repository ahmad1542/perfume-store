const OPEN_INVOICES_API = `${API}/open-purchase-invoices`;

function toMoney(value) {
  return Number(value || 0).toFixed(2);
}

async function fillSuppliers() {
  const el = $('SupplierId');
  if (!el) return;

  el.innerHTML = `<option value="">-- Select Supplier --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.suppliers); } catch { list = []; }

  (Array.isArray(list) ? list : []).forEach(s => {
    const id = s.id ?? s.ID ?? '';
    const name = s.name ?? s.Name ?? '';
    const phone = s.phone ?? s.Phone ?? '';
    if (!id) return;

    const opt = document.createElement('option');
    opt.value = id;
    opt.textContent = name ? (phone ? `${name} (${phone})` : name) : id;
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

async function loadOpenInvoices() {
  const supplierId = $('SupplierId')?.value || '';
  const empty = $('invoiceEmpty');
  const card = $('invoiceTableCard');
  const rows = $('invoiceRows');

  rows.innerHTML = '';

  if (!supplierId) {
    if (empty) {
      empty.style.display = '';
      empty.textContent = 'Choose a supplier to load open purchase invoices.';
    }
    if (card) card.style.display = 'none';
    updateSummary();
    return;
  }

  let list = [];
  try {
    list = await apiGetJson(`${OPEN_INVOICES_API}/${encodeURIComponent(supplierId)}`);
  } catch (e) {
    if (empty) {
      empty.style.display = '';
      empty.textContent = 'Failed to load open purchase invoices.';
    }
    if (card) card.style.display = 'none';
    return;
  }

  if (!Array.isArray(list) || list.length === 0) {
    if (empty) {
      empty.style.display = '';
      empty.textContent = 'This supplier has no unpaid purchase invoices.';
    }
    if (card) card.style.display = 'none';
    updateSummary();
    return;
  }

  list.forEach(x => {
    const invoiceId = x.purchaseInvoiceId ?? x.PurchaseInvoiceId;
    const remainingAmount = Number(x.remainingAmount ?? x.RemainingAmount ?? 0);
    const notes = x.notes ?? x.Notes ?? '';
    const invoiceDate = formatDateOnly(x.invoiceDate ?? x.InvoiceDate ?? '');

    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>#${invoiceId}</td>
      <td>${escapeHtml(invoiceDate)}</td>
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
          data-invoice-id="${invoiceId}"
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
  const applications = [];

  inputs.forEach(input => {
    const appliedAmount = Number(input.value || 0);
    if (appliedAmount > 0) {
      applications.push({
        purchaseInvoiceId: Number(input.dataset.invoiceId),
        appliedAmount,
      });
    }
  });

  return applications;
}

function updateSummary() {
  const voucherAmount = Number($('Amount')?.value || 0);
  const appliedAmount = getApplications().reduce((sum, x) => sum + Number(x.appliedAmount || 0), 0);
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
  return {
    date: $('Date').value || null,
    supplierId: $('SupplierId').value || null,
    moneyAccountID: Number($('MoneyAccountID').value || 0),
    amount: Number($('Amount').value || 0),
    notes: $('Notes').value || null,
    applications: getApplications(),
  };
}

async function createItem() {
  setMsg('pageMsg', 'Saving...', false);
  try {
    const body = buildBodyFromForm();
    const voucherAmount = Number(body.amount || 0);
    const appliedAmount = (body.applications || []).reduce((sum, x) => sum + Number(x.appliedAmount || 0), 0);
    const difference = voucherAmount - appliedAmount;

    if (voucherAmount <= 0) {
      setMsg('pageMsg', 'Voucher amount must be greater than 0', true);
      return;
    }

    if (!body.supplierId) {
      setMsg('pageMsg', 'Supplier is required', true);
      return;
    }

    if (!body.moneyAccountID) {
      setMsg('pageMsg', 'Money account is required', true);
      return;
    }

    if (body.applications.length === 0) {
      setMsg('pageMsg', 'You must allocate at least one invoice', true);
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
  await fillSuppliers();
  await fillMoneyAccounts();
  updateSummary();

  $('SupplierId')?.addEventListener('change', loadOpenInvoices);
  $('Amount')?.addEventListener('input', updateSummary);
  $('btnAutoAllocate')?.addEventListener('click', autoAllocateOldestFirst);
}
