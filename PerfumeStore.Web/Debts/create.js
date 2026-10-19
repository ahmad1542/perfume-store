function buildBodyFromForm() {
  
  const Date = $('Date').value || null;
  const amount = Number($("Amount").value || 0);

  const personIdRaw = $("PersonId").value?.trim() || "";

  return {
    Date: Date,
    Amount: amount,
    moneyAccountId: parseInt($("moneyAccount").value),
    direction: parseInt($("direction").value),
    PersonId: personIdRaw || null,
    Notes: $("Notes").value || null
  };
}

function validateBody(body) {
  if (!body.Amount || body.Amount <= 0) return "Amount is required and must be greater than 0.";

  const linksCount = (body.PersonId ? 1 : 0)

  if (linksCount === 0)
    return "Please link the debt to exactly one: Sales Invoice ID OR Purchase Invoice ID OR Person / Supplier.";

  if (body.moneyAccountId && isNaN(body.moneyAccountId))
    return "Money Account ID must be a valid number.";

  return null;
}

async function createItem() {
  setMsg("pageMsg", "Saving...", false);

  try {
    const body = buildBodyFromForm();
    const err = validateBody(body);
    if (err) {
      setMsg("pageMsg", err, true);
      return;
    }

    await apiSendJson(API, "POST", body);
    window.location.href = "index.html";
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      'pageMsg',
      validationMsg ?? 'Save failed. Check API and payload.',
      true
    );
    console.error(e);
  }
}

async function initPage() {
  try {
    await fillDebtPartiesSelect("PersonId");
    await fillMoneyAccounts("moneyAccount");
    selectSafeMoneyAccount($("moneyAccount"));
  } catch (e) {
    console.error(e);
  }
}

async function fillMoneyAccounts(selectId) {
  const el = $(selectId);
  if (!el) return;
  el.innerHTML = `<option value="">-- Select Account --</option>`;
  let list = [];
  try { list = await apiGetJson(window.API_ENDPOINTS.moneyAccounts); } catch { list = []; }
  (Array.isArray(list) ? list : []).forEach(a => {
    const id = a.id ?? a.ID ?? a.Id ?? "";
    const name = a.accountName ?? a.AccountName ?? ("Account #" + id);
    if (id === "") return;
    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = name;
    el.appendChild(opt);
  });
}

async function fillDebtPartiesSelect(selectId) {
  const el = $(selectId);
  if (!el) return;

  el.innerHTML = `<option value="">-- Select Person / Supplier --</option>`;

  let persons = [];
  let suppliers = [];

  try { persons = await apiGetJson(window.API_ENDPOINTS.persons); } catch { persons = []; }
  try { suppliers = await apiGetJson(window.API_ENDPOINTS.suppliers); } catch { suppliers = []; }

  const seen = new Set();
  const items = [];

  (Array.isArray(persons) ? persons : []).forEach((p) => items.push({ ...p, _partyType: "Person" }));
  (Array.isArray(suppliers) ? suppliers : []).forEach((s) => items.push({ ...s, _partyType: "Supplier" }));

  items.forEach((p) => {
    const id = p.id ?? p.ID ?? p.Id ?? "";
    if (!id || seen.has(id)) return;
    seen.add(id);

    const name = p.name ?? p.Name ?? "";
    const phone = p.phone ?? p.Phone ?? "";
    const type = p._partyType || "Person";

    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = `${type} - ${name ? `${name}${phone ? ` (${phone})` : ""}` : (phone || id)}`;
    el.appendChild(opt);
  });
}
