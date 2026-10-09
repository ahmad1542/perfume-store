
function invoiceSettlementMessage(body, purchase = false, appliedVouchers = 0) {
  if (!Object.keys(body.Products || {}).length) return 'Add at least one invoice item.';
  const prices = purchase ? body.UnitCosts : body.UnitPrices;
  let totalCents = 0;
  for (const [id, qty] of Object.entries(body.Products || {})) {
    if (prices?.[id] == null) return 'Enter the unit price or cost for every invoice item.';
    const price = Number(prices[id]);
    if (!Number.isFinite(price) || price < 0 || Math.abs(price * 100 - Math.round(price * 100)) > 0.000001)
      return 'Unit prices must be non-negative and have at most two decimal places.';
    totalCents += Number(qty) * Math.round(price * 100);
  }
  const paid = Number(body.AmountPaid || 0);
  const debt = body.HasDebt ? Number(body.DebtAmount || 0) : 0;
  if (![paid, debt].every(n => Number.isFinite(n) && n >= 0 && Math.abs(n * 100 - Math.round(n * 100)) < 0.000001))
    return 'Payment and debt amounts must be non-negative and have at most two decimal places.';
  const coveredCents = Math.round(paid * 100) + Math.round(debt * 100) + Math.round(appliedVouchers * 100);
  if (totalCents !== coveredCents) return 'Invoice line total is ₪' + (totalCents / 100).toFixed(2) +
    ', but amount paid plus debt' + (appliedVouchers > 0 ? ' and applied voucher payments' : '') +
    ' is ₪' + (coveredCents / 100).toFixed(2) + '. These amounts must match. Change the unit prices, payment, or debt before saving.';
  return null;
}

function updateInvoiceBalance() {
  const panel = document.getElementById('invoiceBalance');
  if (!panel || typeof buildBodyFromForm !== 'function') return;
  const body = buildBodyFromForm();
  const purchase = panel.dataset.purchase === 'true';
  const prices = purchase ? body.UnitCosts : body.UnitPrices;
  const total = Object.entries(body.Products || {}).reduce((sum, [id, qty]) => sum + Number(qty) * Math.round(Number(prices?.[id] || 0) * 100), 0) / 100;
  const vouchers = Number(window._appliedVoucherPayments || 0);
  const covered = Number(body.AmountPaid || 0) + (body.HasDebt ? Number(body.DebtAmount || 0) : 0);
  const error = invoiceSettlementMessage(body, purchase);
  panel.dataset.balanced = String(!error);
  panel.textContent = 'Line total: ₪' + total.toFixed(2) + ' · Payment + debt' + ': ₪' + covered.toFixed(2) +
    (error ? ' · Not balanced — correct the amounts before saving.' : ' · Balanced') +
    (vouchers > 0 ? ' · Later vouchers: ₪' + vouchers.toFixed(2) + ' · Remaining debt: ₪' + Math.max(0, Number(body.DebtAmount || 0) - vouchers).toFixed(2) + ' · Credit / refund due: ₪' + Math.max(0, vouchers - Number(body.DebtAmount || 0)).toFixed(2) : '');
}
if (typeof document !== 'undefined') {
  document.addEventListener('input', updateInvoiceBalance);
  document.addEventListener('change', updateInvoiceBalance);
  document.addEventListener('DOMContentLoaded', updateInvoiceBalance);
}

function showInvoiceCreditNotice(item, kind, id) {
  const credit = Number(item.creditRefundDue || 0);
  if (credit <= 0) return;
  const key = 'invoice-credit-' + kind + '-' + id + '-' + credit + '-' + item.itemsTotal + '-' + item.appliedVoucherPayments;
  try { if (localStorage.getItem(key)) return; } catch {}
  const panel = document.createElement('div');
  panel.className = 'invoice-balance';
  panel.setAttribute('role', 'status');
  panel.textContent = '₪' + credit.toFixed(2) + ' credit / refund due. No money account balance was changed for this credit.';
  const button = document.createElement('button');
  button.type = 'button'; button.className = 'btn-light'; button.textContent = 'Dismiss';
  button.addEventListener('click', () => { try { localStorage.setItem(key, 'dismissed'); } catch {} panel.remove(); });
  panel.appendChild(button);
  document.getElementById('details').before(panel);
}
