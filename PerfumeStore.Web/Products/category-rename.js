let categoryToRename = null;
let categoryRenameBusy = false;

async function openCategoryRename() {
  const id = $('categoryId').value;
  if (!id) { setMsg('pageMsg', 'Select the category you want to rename first.', true); return; }
  categoryToRename = null;
  $('renameCategoryName').value = '';
  $('btnSaveCategoryName').disabled = true;
  $('renameCategoryDialog').showModal();
  setMsg('renameCategoryMsg', 'Loading category...');
  try {
    const category = await apiGetJson(`${CATEGORIES_API}/${encodeURIComponent(id)}`);
    categoryToRename = { id, description: category.description ?? category.Description ?? null };
    $('renameCategoryName').value = category.name ?? category.Name ?? '';
    $('btnSaveCategoryName').disabled = false;
    setMsg('renameCategoryMsg', '');
    $('renameCategoryName').focus();
  } catch (e) { setMsg('renameCategoryMsg', getFriendlyMessage(e) || 'Could not load the category. Close and try again.', true); }
}

async function saveCategoryName() {
  if (!categoryToRename || categoryRenameBusy) return;
  const name = $('renameCategoryName').value.trim();
  if (!name) { setMsg('renameCategoryMsg', 'Category name is required.', true); return; }
  categoryRenameBusy = true;
  $('btnSaveCategoryName').disabled = true;
  setMsg('renameCategoryMsg', 'Saving...');
  try {
    await apiSendJson(`${CATEGORIES_API}/${encodeURIComponent(categoryToRename.id)}`, 'PATCH', {
      name, description: categoryToRename.description
    });
    // Update the same option without disturbing the product's other fields.
    const option = Array.from($('categoryId').options).find(o => o.value === categoryToRename.id);
    if (option) option.textContent = name;
    $('renameCategoryDialog').close();
    setMsg('pageMsg', 'Category renamed for all products in this category.');
  } catch (e) { setMsg('renameCategoryMsg', getFriendlyMessage(e) || 'Category rename failed.', true); }
  finally { categoryRenameBusy = false; $('btnSaveCategoryName').disabled = false; }
}

document.addEventListener('DOMContentLoaded', () => {
  $('btnRenameCategory').addEventListener('click', openCategoryRename);
  $('btnCancelCategoryRename').addEventListener('click', () => $('renameCategoryDialog').close());
  $('renameCategoryForm').addEventListener('submit', e => { e.preventDefault(); saveCategoryName(); });
});
