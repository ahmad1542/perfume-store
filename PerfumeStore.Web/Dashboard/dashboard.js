const sidebar = document.getElementById('sidebar');
const sidebarBackdrop = document.getElementById('sidebar-backdrop');
const toggleBtn = document.getElementById('toggleSidebar');

let salesPurchasesChart = null;
let profitChart = null;
let brandChart = null;

const fmtMoney = (n) => '₪' + Number(n ?? 0).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });
const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  if (Number.isNaN(date.getTime())) return dateStr;
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
};

function setDelta(elId, pct) {
  const el = document.getElementById(elId);
  if (!el) return;

  const value = Number(pct || 0);
  const up = value >= 0;
  el.classList.toggle('up', up);
  el.classList.toggle('down', !up);
  el.innerHTML = up
    ? `<i class="bi bi-arrow-up-right"></i> +${value.toFixed(1)}%`
    : `<i class="bi bi-arrow-down-right"></i> ${value.toFixed(1)}%`;
}

function buildStatusBadge(status) {
  const safe = escapeHtml(status || '');
  return `<span class="badge text-bg-secondary">${safe}</span>`;
}

function wireActions() {
  document.getElementById('btnNewSale')?.addEventListener('click', () => {
    window.location.href = '../SalesInvoices/create.html';
  });

  document.getElementById('btnNewPurchase')?.addEventListener('click', () => {
    window.location.href = '../PurchaseInvoices/create.html';
  });

  document.getElementById('btnAddExpense')?.addEventListener('click', () => {
    window.location.href = '../Expenses/create.html';
  });

  document.getElementById('btnTransfer')?.addEventListener('click', () => {
    window.location.href = '../MoneyTransactions/create.html';
  });

  document.getElementById('btnRefresh')?.addEventListener('click', async () => {
    await loadDashboard();
  });
}

function renderAlerts(alerts) {
  const alertsWrap = document.getElementById('alertsList');
  if (!alertsWrap) return;

  alertsWrap.innerHTML = '';

  if (!alerts || alerts.length === 0) {
    alertsWrap.innerHTML = '<div class="empty-state">No alerts right now.</div>';
    return;
  }

  const toneMap = {
    info: 'text-info',
    warning: 'text-warning',
    danger: 'text-danger'
  };

  alerts.forEach(alert => {
    const div = document.createElement('button');
    div.type = 'button';
    div.className = 'alert-card text-start';
    div.innerHTML = `
      <div class="d-flex gap-3 align-items-start justify-content-between">
        <div class="d-flex gap-2 align-items-start">
          <i class="bi ${escapeHtml(alert.icon || 'bi-bell')} ${toneMap[alert.tone] || ''}" style="font-size:1.15rem;"></i>
          <div>
            <div style="font-weight:800;">${escapeHtml(alert.title)}</div>
            <div style="color:rgba(232,238,252,.65); font-size:.9rem;">${escapeHtml(alert.description)}</div>
          </div>
        </div>
        <div class="alert-count">${Number(alert.count || 0)}</div>
      </div>
    `;

    div.addEventListener('click', () => {
      if (alert.navigateUrl) {
        window.location.href = alert.navigateUrl;
      }
    });

    alertsWrap.appendChild(div);
  });
}

function renderExpenseBreakdown(expenses) {
  const wrap = document.getElementById('expenseBreakdownList');
  if (!wrap) return;

  wrap.innerHTML = '';

  if (!expenses || expenses.length === 0) {
    wrap.innerHTML = '<div class="empty-state">No expenses recorded this month.</div>';
    return;
  }

  expenses.forEach(item => {
    const div = document.createElement('div');
    div.className = 'expense-row';
    div.innerHTML = `
      <div class="expense-row-name">${escapeHtml(item.name)}</div>
      <div class="expense-row-value">${fmtMoney(item.value)}</div>
    `;
    wrap.appendChild(div);
  });
}

function renderActivity(activity) {
  const tbody = document.getElementById('activityRows');
  const empty = document.getElementById('activityEmpty');
  if (!tbody || !empty) return;

  tbody.innerHTML = '';

  if (!activity || activity.length === 0) {
    empty.classList.remove('d-none');
    return;
  }

  empty.classList.add('d-none');

  activity.slice(0, 10).forEach(item => {
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${escapeHtml(formatDate(item.date))}</td>
      <td>${escapeHtml(item.type)}</td>
      <td>${escapeHtml(item.reference)}</td>
      <td>${escapeHtml(item.party || '-')}</td>
      <td class="text-end">${fmtMoney(item.amount)}</td>
      <td>${buildStatusBadge(item.status)}</td>
      <td class="text-end">
        <button class="btnx btn-sm js-view-activity">View</button>
      </td>
    `;

    tr.querySelector('.js-view-activity')?.addEventListener('click', () => {
      if (item.viewUrl) {
        window.location.href = item.viewUrl;
      }
    });

    tbody.appendChild(tr);
  });
}

function renderCharts(data) {
  const salesPurchasesCtx = document.getElementById('salesPurchasesChart');
  const profitCtx = document.getElementById('profitChart');
  const brandCtx = document.getElementById('brandChart');

  if (salesPurchasesChart) salesPurchasesChart.destroy();
  if (profitChart) profitChart.destroy();
  if (brandChart) brandChart.destroy();

  salesPurchasesChart = new Chart(salesPurchasesCtx, {
    type: 'line',
    data: {
      labels: data.daily?.labels || [],
      datasets: [
        { label: 'Sales', data: data.daily?.sales || [], tension: 0.35, borderWidth: 2, pointRadius: 0 },
        { label: 'Purchases', data: data.daily?.purchases || [], tension: 0.35, borderWidth: 2, pointRadius: 0 }
      ]
    },
    options: {
      responsive: true,
      plugins: { legend: { labels: { color: '#e8eefc' } } },
      scales: {
        x: { ticks: { color: 'rgba(232,238,252,.7)' }, grid: { color: 'rgba(255,255,255,.06)' } },
        y: { ticks: { color: 'rgba(232,238,252,.7)' }, grid: { color: 'rgba(255,255,255,.06)' } }
      }
    }
  });

  profitChart = new Chart(profitCtx, {
    type: 'line',
    data: {
      labels: data.profit?.labels || [],
      datasets: [{ label: 'Profit', data: data.profit?.values || [], tension: 0.35, borderWidth: 2, pointRadius: 2 }]
    },
    options: {
      responsive: true,
      plugins: { legend: { labels: { color: '#e8eefc' } } },
      scales: {
        x: { ticks: { color: 'rgba(232,238,252,.7)' }, grid: { color: 'rgba(255,255,255,.06)' } },
        y: { ticks: { color: 'rgba(232,238,252,.7)' }, grid: { color: 'rgba(255,255,255,.06)' } }
      }
    }
  });

  brandChart = new Chart(brandCtx, {
    type: 'bar',
    data: {
      labels: (data.brands || []).map(x => x.name),
      datasets: [{ label: 'Sales', data: (data.brands || []).map(x => x.value), borderWidth: 1 }]
    },
    options: {
      responsive: true,
      plugins: { legend: { labels: { color: '#e8eefc' } } },
      scales: {
        x: { ticks: { color: 'rgba(232,238,252,.7)' }, grid: { color: 'rgba(255,255,255,.06)' } },
        y: { ticks: { color: 'rgba(232,238,252,.7)' }, grid: { color: 'rgba(255,255,255,.06)' } }
      }
    }
  });
}

function bindDashboard(data) {
  document.getElementById('monthLabel').textContent = data.monthLabel || 'This Month';
  document.getElementById('salesValue').textContent = fmtMoney(data.kpis?.sales?.value);
  document.getElementById('purchasesValue').textContent = fmtMoney(data.kpis?.purchases?.value);
  document.getElementById('earningsValue').textContent = fmtMoney(data.kpis?.earnings?.value);
  document.getElementById('cashValue').textContent = fmtMoney(data.kpis?.cash?.value);

  setDelta('salesDelta', data.kpis?.sales?.deltaPct || 0);
  setDelta('purchasesDelta', data.kpis?.purchases?.deltaPct || 0);
  setDelta('earningsDelta', data.kpis?.earnings?.deltaPct || 0);

  renderAlerts(data.alerts || []);
  renderExpenseBreakdown(data.expenses || []);
  renderActivity(data.activity || []);
  renderCharts(data);
}

async function loadDashboard() {
  const message = document.getElementById('dashboardMsg');
  try {
    const url = `${window.APP_CONFIG.API_BASE_URL}/Dashboard`;
    const data = await apiGetJson(url);
    bindDashboard(data);
    if (message) message.hidden = true;
  } catch (err) {
    console.error(err);
    if (message) {
      message.textContent = 'Could not refresh the dashboard. Displayed values may be out of date. Please try Refresh again.';
      message.hidden = false;
    }
  }
}

toggleBtn?.addEventListener('click', () => {
  sidebar.classList.toggle('open');
  sidebarBackdrop.style.display = sidebar.classList.contains('open') ? 'block' : 'none';
});

sidebarBackdrop?.addEventListener('click', () => {
  sidebar.classList.remove('open');
  sidebarBackdrop.style.display = 'none';
});

wireActions();
loadDashboard();
