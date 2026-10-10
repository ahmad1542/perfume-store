function selectedProductUnitCost() {
  return $('ProductID')?.selectedOptions?.[0]?.dataset.unitCost ?? '';
}

function existingInventoryUnitCost(item) {
  const batches = (item.batches ?? item.Batches ?? []).filter(b => Number(b.remainingQuantity ?? b.RemainingQuantity) > 0);
  batches.sort((a,b) => String(b.receivedAt ?? b.ReceivedAt ?? '').localeCompare(String(a.receivedAt ?? a.ReceivedAt ?? '')) || Number(b.id ?? b.ID ?? 0) - Number(a.id ?? a.ID ?? 0));
  const latest = batches[0];
  return latest ? latest.unitCost ?? latest.UnitCost ?? selectedProductUnitCost() : selectedProductUnitCost();
}

function prefillProductUnitCost() {
  const input = $('AddedUnitCost');
  if (input) input.value = selectedProductUnitCost();
}
