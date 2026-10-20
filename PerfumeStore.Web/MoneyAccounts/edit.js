async function initEdit() {
  const id = getQueryParam('id');
  if (!id) {
    setMsg('pageMsg', 'Missing id in URL.', true);
    return;
  }

  if (typeof initPage === 'function') await initPage();

  setMsg('pageMsg', 'Loading...', false);

  try {
    const item = await apiGetJson(`${API}/${encodeURIComponent(id)}`);
    if ($('AccountName')) $('AccountName').value = item.accountName ?? item.AccountName ?? item.accountname ?? item.Accountname ?? '';
    if ($('CurrentBalance')) $('CurrentBalance').value = item.currentBalance ?? item.CurrentBalance ?? item.currentbalance ?? item.Currentbalance ?? '';
    if ($('Notes')) $('Notes').value = item.notes ?? item.Notes ?? '';

    setMsg('pageMsg', '');
  } catch (e) {
    setMsg('pageMsg', 'Failed to load record from API.', true);
    console.error(e);
    return;
  }

  const btn = $('btnSave');
  if (btn) btn.addEventListener('click', () => updateItem(id));
}

function buildBodyFromForm() {
  const body = {};
  body['AccountName'] = $('AccountName').value || null;
  body['CurrentBalance'] = Number($('CurrentBalance').value || 0);
  body['Notes'] = $('Notes').value || null;
  return body;
}

async function updateItem(id) {
  setMsg('pageMsg', 'Saving...', false);
  try {
    const body = buildBodyFromForm();
    await apiSendJson(`${API}/${encodeURIComponent(id)}`, 'PATCH', body);
    window.location.href = 'index.html';
  } catch (e) {
    const validationMsg = getFriendlyMessage(e);

    setMsg(
      'pageMsg',
      validationMsg ?? 'Update failed. Please check your input and try again.',
      true
    );
    console.error(e);
  }
}
