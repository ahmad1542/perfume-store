const sidebar = document.getElementById('sidebar');
const sidebarBackdrop = document.getElementById('sidebar-backdrop');
const toggleBtn = document.getElementById('toggleSidebar');

let salesPurchasesChart = null;
let profitChart = null;
let brandChart = null;
let dashboardRequest = 0;
function initializePeriodSelectors() {
  const month = document.getElementById('dashboardMonth');
  const year = document.getElementById('dashboardYear');
  if (!month || !year) return;
  const now = new Date();
  for (let m = 1; m <= 12; m++) {
    const option = document.createElement('option'); option.value = m;
    option.textContent = new Date(2021, m - 1, 1).toLocaleString(undefined, { month: 'long' }); month.appendChild(option);
  }
  for (let y = now.getFullYear(); y >= 2021; y--) {
    const option = document.createElement('option'); option.value = y; option.textContent = y; year.appendChild(option);
  }
  month.value = now.getMonth() + 1; year.value = now.getFullYear();
  month.addEventListener('change', loadDashboard); year.addEventListener('change', loadDashboard);
}


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
    wrap.innerHTML = '<div class="empty-state">No expenses recorded in the selected month.</div>';
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

function chartOptions(daily = false) {
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false, axis: 'x' },
    plugins: {
      legend: { labels: { color: '#e8eefc', usePointStyle: true, boxWidth: 8 } },
      tooltip: {
        callbacks: {
          title: items => daily ? 'Day ' + items[0].label : items[0].label,
          label: item => item.dataset.label + ': ' + fmtMoney(item.parsed.y)
        }
      }
    },
    scales: {
      x: { ticks: { color: 'rgba(232,238,252,.7)', maxRotation: 0, autoSkip: true, maxTicksLimit: daily ? 16 : 5 }, grid: { display: false } },
      y: { ticks: { color: 'rgba(232,238,252,.7)', callback: value => fmtMoney(value) }, grid: { color: 'rgba(255,255,255,.06)' } }
    }
  };
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
        { label: 'Sales', borderColor: '#61dafb', backgroundColor: '#61dafb', data: data.daily?.sales || [], tension: 0, borderWidth: 2, pointRadius: 3, pointHoverRadius: 6, pointHitRadius: 18 },
        { label: 'Purchases', borderColor: '#f5b970', backgroundColor: '#f5b970', data: data.daily?.purchases || [], tension: 0, borderWidth: 2, pointRadius: 3, pointHoverRadius: 6, pointHitRadius: 18 }
      ]
    },
    options: chartOptions(true)
  });

  profitChart = new Chart(profitCtx, {
    type: 'line',
    data: {
      labels: data.profit?.labels || [],
      datasets: [{ label: 'Daily net profit', borderColor: '#79d6ad', backgroundColor: '#79d6ad', data: data.profit?.values || [], tension: 0, borderWidth: 2, pointRadius: 3, pointHoverRadius: 6, pointHitRadius: 18 }]
    },
    options: chartOptions(true)
  });

  brandChart = new Chart(brandCtx, {
    type: 'bar',
    data: {
      labels: (data.brands || []).map(x => x.name),
      datasets: [{ label: 'Sales', backgroundColor: 'rgba(97,218,251,.45)', borderColor: '#61dafb', borderRadius: 6, data: (data.brands || []).map(x => x.value), borderWidth: 1 }]
    },
    options: chartOptions(false)
  });
}

function bindDashboard(data) {
  document.getElementById('monthLabel').textContent = data.monthLabel || 'This Month';
  document.getElementById('salesValue').textContent = fmtMoney(data.kpis?.sales?.value);
  document.getElementById('purchasesValue').textContent = fmtMoney(data.kpis?.purchases?.value);
  document.getElementById('earningsValue').textContent = fmtMoney(data.netProfit);
  document.getElementById('monthNetProfit').textContent = fmtMoney(data.netProfit);
  document.getElementById('profitRevenue').textContent = fmtMoney(data.salesRevenue);
  document.getElementById('profitCost').textContent = fmtMoney(data.costOfGoodsSold);
  document.getElementById('profitGross').textContent = fmtMoney(data.grossProfit);
  document.getElementById('profitExpenses').textContent = fmtMoney(data.expenseTotal);
  document.getElementById('expenseTotal').textContent = fmtMoney(data.expenseTotal);
  document.getElementById('profitEstimate').hidden = !data.hasEstimatedProfit;
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
  const request = ++dashboardRequest;
  const message = document.getElementById('dashboardMsg');
  try {
    const month = document.getElementById('dashboardMonth')?.value;
    const year = document.getElementById('dashboardYear')?.value;
    const period = month && year ? '?year=' + encodeURIComponent(year) + '&month=' + encodeURIComponent(month) : '';
    const url = window.APP_CONFIG.API_BASE_URL + '/Dashboard' + period;
    const data = await apiGetJson(url);
    if (request !== dashboardRequest) return;
    bindDashboard(data);
    if (message) message.hidden = true;
  } catch (err) {
    if (request !== dashboardRequest) return;
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

initializePeriodSelectors();
wireActions();
loadDashboard();
