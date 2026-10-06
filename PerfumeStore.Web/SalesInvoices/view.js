async function loadView() {
  const id = getQueryParam('id');
  if (!id) {
    setMsg('pageMsg', 'Missing id in URL.', true);
    return;
  }

  setMsg('pageMsg', 'Loading...', false);

  try {
    const item = await apiGetJson(`${API}/${encodeURIComponent(id)}`);

    const detailsLines = [];
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">ID</div><div>${escapeHtml(item.id ?? item.ID ?? '')}</div>`);
    const rawDate = item.date ?? item.Date ?? '';
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">Date</div><div>${escapeHtml(formatDateOnly(rawDate))}</div>`);
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">Customer</div><div>${escapeHtml(item.customerName ?? item.CustomerName ?? '-')}</div>`);
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">Amount Paid</div><div>${escapeHtml(item.amountPaid ?? item.AmountPaid ?? 0)}</div>`);
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">Debt Amount</div><div>${escapeHtml(item.debtAmount ?? item.DebtAmount ?? 0)}</div>`);
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">Products Count</div><div>${escapeHtml(item.productsCount ?? item.ProductsCount ?? 0)}</div>`);
    detailsLines.push(`<div style="color:rgba(232,238,252,.75);font-weight:800;">Notes</div><div>${escapeHtml(item.notes ?? item.Notes ?? '-')}</div>`);

    $('details').innerHTML = detailsLines.join('');

    const rows = $('productsRows');
    if (rows) {
      rows.innerHTML = '';
      const products = item.products ?? item.Products ?? [];

      if (!Array.isArray(products) || products.length === 0) {
        rows.innerHTML = `<tr><td colspan="7">No products found.</td></tr>`;
      } else {
        products.forEach((p, index) => {
          const tr = document.createElement('tr');
          tr.innerHTML = `
            <td>${index + 1}</td>
            <td>${escapeHtml(p.productName ?? p.ProductName ?? p.productID ?? p.ProductID ?? '-')}</td>
            <td>${escapeHtml(p.quantity ?? p.Quantity ?? 0)}</td>
            <td>${escapeHtml(p.unitPrice ?? p.UnitPrice ?? 'Unknown')}${(p.isPriceEstimated ?? p.IsPriceEstimated) ? ' (estimated)' : ''}</td>
            <td>${escapeHtml(p.lineTotal ?? p.LineTotal ?? 'Unknown')}</td>
            <td>${escapeHtml(p.costTotal ?? p.CostTotal ?? 'Unknown')}${(p.isCostEstimated ?? p.IsCostEstimated) ? ' (estimated)' : ''}</td>
            <td>${escapeHtml(p.grossProfit ?? p.GrossProfit ?? 'Unknown')}</td>
          `;
          rows.appendChild(tr);
        });
      }
    }

    setMsg('pageMsg', '');
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      'pageMsg',
      validationMsg ?? 'Failed to load record from API. Check console for details.',
      true
    );
    console.error(e);
  }
}
