function buildBodyFromForm() {
  return {
    Date: $('Date').value || null,
    ExpenseTypeId: Number($('ExpenseTypeID').value || 0),
    Amount: Number($('Amount').value || 0),
    MoneyAccountID: Number($('MoneyAccountID').value || 0),
    Notes: $('Notes').value?.trim() || null,
  };
}

async function createItem() {
  setMsg('pageMsg', 'Saving...', false);
  try {
    const body = buildBodyFromForm();
    await apiSendJson(API, 'POST', body);
    window.location.href = 'index.html';
  } catch (e) {
    setMsg('pageMsg', getFriendlyMessage(e), true);
    console.error(e);
  }
}

async function initPage() {
  try {
    const today = new Date().toISOString().split('T')[0];
    if ($('Date') && !$('Date').value) $('Date').value = today;

    await fillMoneyAccounts('MoneyAccountID');
    selectSafeMoneyAccount($('MoneyAccountID'));
    await fillExpenseTypes();
    wireExpenseTypeCreation();
  } catch (e) {
    console.error(e);
  }
}
