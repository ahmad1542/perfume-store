// Page-specific filters; URL state is also retained for a return from View/Edit.
const RecordFilters = (() => {
  const segments = location.pathname.split('/').filter(Boolean);
  const page = segments.at(-1)?.endsWith('.html') ? segments.at(-2) : segments.at(-1);
  const initial = new URLSearchParams(location.search);
  const historyKind = initial.get('kind') === 'profit' ? 'profit' : 'cash';
  const storageKey = 'perfume:filters:' + page + (page === 'FinancialHistory' ? ':' + historyKind : '');
  try {
    const saved = JSON.parse(sessionStorage.getItem(storageKey) || 'null');
    if (saved?.returning && !initial.size) {
      window.history.replaceState(null, '', '?' + saved.query);
      initial.forEach((_, key) => initial.delete(key));
      new URLSearchParams(saved.query).forEach((value, key) => initial.append(key, value));
    }
    if (saved?.returning) sessionStorage.setItem(storageKey, JSON.stringify({ ...saved, returning: false }));
  } catch { /* Filters still work when browser storage is unavailable. */ }
  const host = document.getElementById('advancedFilters');
  const configs = {
    SalesInvoices: [['categoryIds','Categories','ProductCategories'],['brandId','Brand','Brands'],['productId','Product','Products'],['partyId','Customer','Customers']],
    PurchaseInvoices: [['categoryIds','Categories','ProductCategories'],['brandId','Brand','Brands'],['productId','Product','Products'],['partyId','Supplier','Suppliers']],
    Expenses: [['accountId','Money account','MoneyAccounts']],
    Debts: [['partyType','Party type',[['customer','Customer'],['supplier','Supplier'],['person','Person']]],['partyId','Party','parties'],['direction','Direction',[['1','Receivable'],['2','Payable']]],['status','Status',[['open','Open'],['settled','Settled'],['all','All']], 'open']],
    ReceiptVouchers: [['partyType','Party type',[['customer','Customer'],['person','Person']]],['partyId','Party','receiptParties'],['accountId','Money account','MoneyAccounts'],['invoiceId','Sales invoice #','number']],
    PaymentVouchers: [['partyId','Supplier','Suppliers'],['accountId','Money account','MoneyAccounts'],['invoiceId','Purchase invoice #','number']],
    MoneyTransactions: [['fromAccountId','From account','MoneyAccounts'],['toAccountId','To account','MoneyAccounts']],
    Inventory: [['brandId','Brand','Brands'],['categoryIds','Categories','ProductCategories'],['stockStatus','Stock status',[['in-stock','In stock'],['out-of-stock','Out of stock'],['low-stock','Low stock']]]],
    FinancialHistory: [['sourceType', historyKind === 'cash' ? 'Transaction type' : 'Profit entries', historyKind === 'cash'
      ? [['SalesInvoices','Sale payments'],['PurchaseInvoices','Purchase payments'],['Expenses','Expenses'],['ReceiptVouchers','Receipt vouchers'],['PaymentVouchers','Payment vouchers'],['MoneyTransactions','Transfers'],['Debts','Standalone debts'],['MoneyAccounts','Account balances']]
      : [['SalesInvoices','Sales'],['Expenses','Expenses'],['ReceiptVouchers','Debt collections (cash only)'],['PaymentVouchers','Debt payments (cash only)']]], ...(historyKind === 'profit' ? [['correctionsOnly','Corrections only','checkbox']] : [])]
  };
  const config = configs[page] || [], controls = new Map();
  let legacyRestored = false, searchRestored = false, partyOptions = [];
  const get = id => document.getElementById(id);
  function reload() {
    if (page === 'FinancialHistory') window.loadHistory?.();
    else window.loadList?.(get('searchInput')?.value || '');
  }
  function value(key) { const c = controls.get(key); return c?.type === 'checkbox' ? c.checked : c?.value || ''; }
  function updateCategories() {
    const details = controls.get('categoryIds'); if (!details) return;
    const count = details.querySelectorAll('input:checked').length;
    details.querySelector('summary').textContent = count ? `${count} categories` : 'All categories';
  }
  function refreshParties() {
    const select = controls.get('partyId'); if (!select || !partyOptions.length) return;
    const selected = select.value, type = value('partyType');
    select.replaceChildren(new Option('All parties',''));
    partyOptions.filter(x => !type || x.type === type).forEach(x => select.add(new Option(`${x.type}: ${x.name}`,x.id)));
    if ([...select.options].some(x => x.value === selected)) select.value = selected;
  }
  async function lookup(resource) {
    const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}/${resource}`, { headers: { Accept: 'application/json' } });
    if (!response.ok) throw Error(`Could not load ${resource} filter choices. Refresh to retry.`);
    const list = await response.json(); return Array.isArray(list) ? list : [];
  }
  const cache = new Map();
  function options(resource) { if (!cache.has(resource)) cache.set(resource,lookup(resource)); return cache.get(resource); }
  function restoreLegacy() {
    if (!searchRestored && get('searchInput')) {
      if (initial.has('search')) get('searchInput').value = initial.get('search');
      searchRestored = true;
    }
    const types = [...document.querySelectorAll('.expense-type-filter-checkbox')];
    if (!legacyRestored && types.length && initial.has('expenseTypeIds')) {
      const chosen = initial.getAll('expenseTypeIds'); types.forEach(x => x.checked = chosen.includes(x.value));
      if (get('expenseTypeAllCheckbox')) get('expenseTypeAllCheckbox').checked = types.every(x => x.checked);
      if (typeof updateExpenseTypeDropdownText === 'function') updateExpenseTypeDropdownText();
      legacyRestored = true;
    }
  }
  function remember(params) {
    const saved = new URLSearchParams(params);
    if (page === 'FinancialHistory') saved.set('kind',historyKind);
    if (get('fromDateInput') && !saved.has('fromDate') && !saved.has('toDate')) saved.set('allDates','1');
    if (page === 'Expenses') {
      saved.delete('expenseTypeIds');
      const boxes = [...document.querySelectorAll('.expense-type-filter-checkbox')];
      boxes.filter(x => x.checked).forEach(x => saved.append('expenseTypeIds',x.value));
      if (boxes.length && !boxes.some(x => x.checked)) saved.append('expenseTypeIds','none');
    }
    try { sessionStorage.setItem(storageKey, JSON.stringify({query:saved.toString(),returning:false})); } catch {}
    window.history.replaceState(null,'',saved.size ? '?' + saved : location.pathname);
  }
  function append(params, save = true) {
    restoreLegacy();
    for (const [key,,type] of config) {
      params.delete(key);
      const control = controls.get(key); if (!control) continue;
      if (key === 'categoryIds') control.querySelectorAll('input:checked').forEach(x => params.append(key,x.value));
      else if (type === 'checkbox') { if (control.checked) params.set(key,'true'); }
      else if (control.value) params.set(key,control.value);
    }
    if (save) remember(params); return params;
  }
  function clear() {
    for (const [key,,type,defaultValue] of config) {
      const control = controls.get(key); if (!control) continue;
      if (key === 'categoryIds') control.querySelectorAll('input').forEach(x => x.checked = false);
      else if (type === 'checkbox') control.checked = false;
      else control.value = defaultValue || '';
    }
    if (get('searchInput')) get('searchInput').value = '';
    const now = new Date();
    const local = date => `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;
    if (get('fromDateInput')) get('fromDateInput').value = local(new Date(now.getFullYear(),now.getMonth(),1));
    if (get('toDateInput')) get('toDateInput').value = local(new Date(now.getFullYear(),now.getMonth()+1,0));
    if (page === 'FinancialHistory') { get('year').value=now.getFullYear();get('month').value=now.getMonth()+1;get('account').value=''; }
    document.querySelectorAll('.expense-type-filter-checkbox').forEach(x => x.checked = true);
    if (get('expenseTypeAllCheckbox')) get('expenseTypeAllCheckbox').checked = true;
    if (typeof updateExpenseTypeDropdownText === 'function') updateExpenseTypeDropdownText();
    legacyRestored = true; updateCategories(); refreshParties(); reload();
  }
  const ready = (async () => {
    if (!host) return;
    const tasks=[];
    for (const [key,title,type,defaultValue] of config) {
      const label=document.createElement('div');label.className='filter-field';
      const heading=document.createElement('label');heading.textContent=title;heading.htmlFor='filter-'+key;label.appendChild(heading);
      let control;
      if (key === 'categoryIds') {
        control=document.createElement('details');control.className='category-filter';
        const summary=document.createElement('summary');summary.textContent='All categories';control.appendChild(summary);
      } else if (type === 'checkbox' || type === 'number') {
        control=document.createElement('input');control.type=type; if(type==='number'){control.min='1';control.step='1';control.placeholder='Any invoice';}
      } else {
        control=document.createElement('select');
        if(!defaultValue) control.add(new Option('All',''));
      }
      control.id='filter-'+key;control.setAttribute('aria-label',title);controls.set(key,control);label.appendChild(control);host.appendChild(label);
      control.addEventListener('change',()=>{if(key==='partyType') refreshParties();updateCategories();reload();});
      if(Array.isArray(type)) {type.forEach(([id,name])=>control.add(new Option(name,id)));control.value=(key==='partyType'?initial.get(key)?.toLowerCase():initial.get(key))||defaultValue||'';}
      else if(type==='checkbox') control.checked=initial.get(key)==='true';
      else if(type==='number') control.value=initial.get(key)||'';
      else tasks.push((async()=>{
        if(type==='parties'||type==='receiptParties') {
          const resources=type==='parties'?['Customers','Suppliers','Persons']:['Customers','Persons'];
          const lists=await Promise.all(resources.map(options));
          partyOptions=lists.flatMap((list,index)=>list.map(x=>({id:String(x.id??x.ID??x.Id),name:x.name??x.Name,type:resources[index]==='Customers'?'customer':resources[index]==='Suppliers'?'supplier':'person'})));
          const seen = new Set(); partyOptions = partyOptions.filter(x => !seen.has(x.id) && seen.add(x.id));
          refreshParties();control.value=initial.get(key)||'';return;
        }
        const list=await options(type);
        for(const x of list){const id=String(x.id??x.ID??x.Id),name=x.name??x.Name??x.accountName??x.AccountName??id;
          if(key==='categoryIds') {const item=document.createElement('label');const box=document.createElement('input');box.type='checkbox';box.value=id;box.checked=initial.getAll(key).includes(id);item.append(box,document.createTextNode(name));control.appendChild(item);}
          else control.add(new Option(name,id));
        }
        if(key!=='categoryIds') control.value=initial.get(key)||'';
      })());
    }
    if(page==='Inventory' && initial.get('lowStock')==='true' && !initial.has('stockStatus')) controls.get('stockStatus').value='low-stock';
    const button=document.createElement('button');button.type='button';button.className='btn-light';button.textContent='Clear filters';button.addEventListener('click',clear);host.appendChild(button);
    try {await Promise.all(tasks);updateCategories();restoreLegacy();}
    catch(error){const text=document.createElement('p');text.className='filter-error';text.textContent=error.message;host.appendChild(text);throw error;}
  })();
  ready.catch(()=>{});
  document.addEventListener('click',event=>{
    const link=event.target.closest?.('a[href]');if(!link)return;
    const target=new URL(link.href,location.href);
    if(target.origin!==location.origin||!/(?:view|edit|create)\.html$/.test(target.pathname))return;
    try {const saved=JSON.parse(sessionStorage.getItem(storageKey)||'null');if(saved)sessionStorage.setItem(storageKey,JSON.stringify({...saved,returning:true}));}catch{}
  });
  const hasLineFilters = () => !!(value('brandId') || value('productId') || controls.get('categoryIds')?.querySelectorAll('input:checked').length);
  return {ready,append,remember,value,clear,restoreLegacy,hasLineFilters};
})();
