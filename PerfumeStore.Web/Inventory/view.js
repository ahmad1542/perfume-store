async function loadView() {
  const id = getQueryParam('id');
  if (!id) {
    setMsg('pageMsg', 'Missing id in URL.', true);
    return;
  }

  setMsg('pageMsg', 'Loading...', false);

  try {
    const item = await apiGetJson(`${API}/${encodeURIComponent(id)}`);
    const lines = [];
    for (const [k, v] of Object.entries(item || {})) {
      if (typeof v === 'object' && v !== null) continue;
      lines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">${escapeHtml(k)}</div><div>${escapeHtml(v)}</div>`);
    }
    $('details').innerHTML = lines.join('');
    const batches = item.batches ?? item.Batches ?? [];
    if (batches.length) {
      const table = document.createElement('div');
      table.className = 'stock-cost-card';
      table.innerHTML = `<div class="stock-cost-header"><h3>Stock by purchase cost</h3><span>Total value <strong>₪${escapeHtml(Number(item.stockCost ?? item.StockCost ?? 0).toFixed(2))}</strong></span></div><p class="stock-cost-caption">Remaining stock grouped by the cost paid for each batch.</p><div class="table-wrapper"><table class="inventory-table stock-cost-table"><thead><tr><th>Source</th><th class="stock-number">Qty remaining</th><th class="stock-number">Unit cost</th><th class="stock-number">Stock value</th></tr></thead><tbody>${batches.map(b => {
        const qty = b.remainingQuantity ?? b.RemainingQuantity;
        const cost = b.unitCost ?? b.UnitCost;
        const estimated = b.isCostEstimated ?? b.IsCostEstimated;
        const source = b.purchaseInvoiceID ?? b.PurchaseInvoiceID;
        const label = source ? `<a class="inventory-link" href="../PurchaseInvoices/view.html?id=${encodeURIComponent(source)}">Purchase #${escapeHtml(source)}</a>` : escapeHtml(b.source ?? b.Source);
        return `<tr><td>${label}${estimated ? '<span class="stock-estimate">Estimated cost</span>' : ''}</td><td class="stock-number">${escapeHtml(qty)}</td><td class="stock-number">₪${escapeHtml(Number(cost).toFixed(2))}</td><td class="stock-number stock-value">₪${escapeHtml((qty * cost).toFixed(2))}</td></tr>`;
      }).join('')}</tbody></table></div>`;
      $('details').appendChild(table);
    }
    setMsg('pageMsg', '');
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      'pageMsg',
      validationMsg ?? 'Failed to load record from API.',
      true
    );
    console.error(e);
  }
}
