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
      table.className = 'table-wrapper table-card';
      table.innerHTML = `<h3>Stock by purchase cost</h3><table><thead><tr><th>Source</th><th>Qty remaining</th><th>Unit cost</th><th>Stock value</th></tr></thead><tbody>${batches.map(b => {
        const qty = b.remainingQuantity ?? b.RemainingQuantity;
        const cost = b.unitCost ?? b.UnitCost;
        const estimated = b.isCostEstimated ?? b.IsCostEstimated;
        const source = b.purchaseInvoiceID ?? b.PurchaseInvoiceID;
        return `<tr><td>${escapeHtml(source ? `Purchase #${source}` : (b.source ?? b.Source))}</td><td>${escapeHtml(qty)}</td><td>${escapeHtml(cost)}${estimated ? ' (estimated)' : ''}</td><td>${escapeHtml((qty * cost).toFixed(2))}</td></tr>`;
      }).join('')}</tbody></table>`;
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
