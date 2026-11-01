const API = window.API_ENDPOINTS.expenses;
const EXPENSE_TYPES_API = window.API_ENDPOINTS.expenseTypes;
const MONEY_ACCOUNTS_API = window.API_ENDPOINTS.moneyAccounts;

document.addEventListener("DOMContentLoaded", async () => {
  const searchBox = $("searchInput");

  let timer = null;

  const triggerLoad = () => {
    clearTimeout(timer);
    timer = setTimeout(() => {
      if (typeof loadList === "function") {
        loadList(searchBox?.value || "");
      }
    }, 350);
  };

  if (searchBox) searchBox.addEventListener("input", triggerLoad);

  initExpenseTypeDropdown();
  await loadExpenseTypeFilters();
});

async function fillExpenseTypes(selectedValue = null) {
  const select = $('ExpenseTypeID');
  if (!select) return;

  select.innerHTML = `<option value="">-- Select expense type --</option>`;
  let types = [];

  try {
    types = await apiGetJson(EXPENSE_TYPES_API);
  } catch {
    types = [];
  }

  (Array.isArray(types) ? types : []).forEach(t => {
    const id = t.id ?? t.ID ?? t.Id ?? '';
    const name = t.name ?? t.Name ?? '';
    if (id === '' || !name) return;

    const option = document.createElement('option');
    option.value = id;
    option.textContent = name;
    select.appendChild(option);
  });

  if (selectedValue !== null && selectedValue !== undefined && selectedValue !== '') {
    select.value = String(selectedValue);
  }
}

async function fillMoneyAccounts(selectId, selectedValue = null) {
  const el = $(selectId);
  if (!el) return;

  el.innerHTML = `<option value="">-- Select money account --</option>`;
  let list = [];
  try {
    list = await apiGetJson(MONEY_ACCOUNTS_API);
  } catch {
    list = [];
  }

  (Array.isArray(list) ? list : []).forEach(a => {
    const id = a.id ?? a.ID ?? a.Id ?? "";
    const name = a.accountName ?? a.AccountName ?? ("Account #" + id);
    if (id === "") return;

    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name;
    el.appendChild(opt);
  });

  if (selectedValue !== null && selectedValue !== undefined && selectedValue !== '') {
    el.value = String(selectedValue);
  }
}

function wireExpenseTypeCreation() {
  const toggleBtn = $('toggleNewExpenseTypeBtn');
  const createBtn = $('btnCreateExpenseType');
  const panel = $('newExpenseTypePanel');
  const nameInput = $('NewExpenseTypeName');

  if (toggleBtn && panel && !toggleBtn.dataset.bound) {
    toggleBtn.dataset.bound = '1';
    toggleBtn.addEventListener('click', () => {
      const isHidden = panel.style.display === 'none' || !panel.style.display;
      panel.style.display = isHidden ? 'grid' : 'none';
      if (isHidden) nameInput?.focus();
      setMsg('expenseTypeMsg', '');
    });
  }

  if (createBtn && !createBtn.dataset.bound) {
    createBtn.dataset.bound = '1';
    createBtn.addEventListener('click', createExpenseTypeInline);
  }
}

async function createExpenseTypeInline() {
  const input = $('NewExpenseTypeName');
  const panel = $('newExpenseTypePanel');
  const name = input?.value?.trim() || '';

  if (!name) {
    setMsg('expenseTypeMsg', 'Please enter an expense type name.', true);
    return;
  }

  setMsg('expenseTypeMsg', 'Creating expense type...', false);

  try {
    const response = await apiSendJson(EXPENSE_TYPES_API, 'POST', { name });
    const createdId = response?.id ?? response?.Id ?? response?.ID ?? null;

    await fillExpenseTypes(createdId);

    if (input) input.value = '';
    if (panel) panel.style.display = 'none';

    setMsg('expenseTypeMsg', 'Expense type created successfully.');
  } catch (e) {
    setMsg('expenseTypeMsg', getFriendlyMessage(e), true);
    console.error(e);
  }
}


function getSelectedExpenseTypeIds() {
  return Array.from(document.querySelectorAll('.expense-type-filter-checkbox:checked'))
    .map(x => Number(x.value))
    .filter(x => Number.isFinite(x) && x > 0);
}

function updateExpenseTypeDropdownText() {
  const textEl = $("expenseTypeDropdownText");
  if (!textEl) return;

  const all = document.querySelectorAll('.expense-type-filter-checkbox');
  const checked = document.querySelectorAll('.expense-type-filter-checkbox:checked');

  if (all.length === 0) {
    textEl.textContent = "Expense types";
    return;
  }

  if (checked.length === all.length) {
    textEl.textContent = "All expense types";
    return;
  }

  if (checked.length === 0) {
    textEl.textContent = "No expense types";
    return;
  }

  if (checked.length === 1) {
    const labelText = checked[0].closest("label")?.querySelector("span")?.textContent?.trim();
    textEl.textContent = labelText || "1 expense type";
    return;
  }

  textEl.textContent = `${checked.length} expense types`;
}

function initExpenseTypeDropdown() {
  const btn = $("expenseTypeDropdownBtn");
  const menu = $("expenseTypeDropdownMenu");
  const allCheckbox = $("expenseTypeAllCheckbox");

  if (btn && !btn.dataset.bound) {
    btn.dataset.bound = "1";
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      menu.classList.toggle("hidden");
      btn.classList.toggle("open", !menu.classList.contains("hidden"));
    });
  }

  if (allCheckbox && !allCheckbox.dataset.bound) {
    allCheckbox.dataset.bound = "1";
    allCheckbox.addEventListener("change", function () {
      document.querySelectorAll(".expense-type-filter-checkbox").forEach(cb => {
        cb.checked = allCheckbox.checked;
      });

      updateExpenseTypeDropdownText();

      if (typeof loadList === "function") {
        loadList($("searchInput")?.value || "");
      }
    });
  }

  if (!document.body.dataset.expenseDropdownBound) {
    document.body.dataset.expenseDropdownBound = "1";

    document.addEventListener("click", function (e) {
      const wrap = document.querySelector(".expense-type-dropdown-wrap");
      if (!wrap || !menu) return;

      if (!wrap.contains(e.target)) {
        menu.classList.add("hidden");
        btn.classList.remove("open");
      }
    });
  }
}

async function loadExpenseTypeFilters() {
  const container = $('expenseTypeFilters');
  const allCheckbox = $('expenseTypeAllCheckbox');

  if (!container) return;

  container.innerHTML = '';

  let types = [];
  try {
    types = await apiGetJson(EXPENSE_TYPES_API);
    types = Array.isArray(types) ? types : [];
  } catch {
    types = [];
  }

  if (types.length === 0) {
    updateExpenseTypeDropdownText();
    if (typeof loadList === 'function') loadList($('searchInput')?.value || '');
    return;
  }

  types.forEach(t => {
    const id = t.id ?? t.ID ?? t.Id ?? '';
    const name = t.name ?? t.Name ?? '';
    if (id === '' || !name) return;

    const label = document.createElement('label');
    label.className = 'checkbox-filter-item';
    label.innerHTML = `
      <input class="expense-type-filter-checkbox" type="checkbox" value="${escapeHtml(id)}" checked />
      <span>${escapeHtml(name)}</span>
    `;
    container.appendChild(label);
  });

  container.querySelectorAll('.expense-type-filter-checkbox').forEach(cb => {
    cb.addEventListener('change', () => {
      const all = container.querySelectorAll('.expense-type-filter-checkbox');
      const checked = container.querySelectorAll('.expense-type-filter-checkbox:checked');

      if (allCheckbox) {
        allCheckbox.checked = all.length > 0 && checked.length === all.length;
      }

      updateExpenseTypeDropdownText();

      if (typeof loadList === 'function') {
        loadList($('searchInput')?.value || '');
      }
    });
  });

  if (allCheckbox) {
    const all = container.querySelectorAll('.expense-type-filter-checkbox');
    const checked = container.querySelectorAll('.expense-type-filter-checkbox:checked');
    allCheckbox.checked = all.length > 0 && checked.length === all.length;
  }

  updateExpenseTypeDropdownText();

  if (typeof loadList === 'function') {
    loadList($('searchInput')?.value || '');
  }
}