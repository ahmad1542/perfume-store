using PerfumeStore.Domain.Exceptions;

namespace PerfumeStore.Domain.Entities;

public static class FifoCosting {
    // Replaying the tracked ledger allows a corrected supplier cost/quantity to
    // update affected sale costs without changing the agreed selling prices.
    public static List<SalesCostAllocation> Recalculate(
        IEnumerable<InventoryBatch> inventoryBatches, IEnumerable<SalesInvoiceItem> sales) {
        var batches = inventoryBatches.OrderBy(b => b.ReceivedAt).ThenBy(b => b.ID).ToList();
        foreach (var batch in batches) batch.RemainingQuantity = batch.OriginalQuantity;
        var allocations = new List<SalesCostAllocation>();
        foreach (var sale in sales.OrderBy(s => s.SalesInvoiceID).ThenBy(s => s.ID)) {
            var needed = sale.Quantity;
            sale.CostTotal = 0;
            sale.IsCostEstimated = false;
            foreach (var batch in batches.Where(b => b.RemainingQuantity > 0)) {
                if (needed <= 0) break;
                var quantity = Math.Min(needed, batch.RemainingQuantity);
                batch.RemainingQuantity -= quantity;
                needed -= quantity;
                sale.CostTotal += quantity * batch.UnitCost;
                sale.IsCostEstimated |= batch.IsCostEstimated;
                allocations.Add(new SalesCostAllocation {
                    SalesInvoiceItemID = sale.ID,
                    InventoryBatchID = batch.ID,
                    Quantity = quantity,
                    UnitCost = batch.UnitCost,
                    IsCostEstimated = batch.IsCostEstimated
                });
            }
            if (needed > 0) {
                throw new BusinessRuleException($"The corrected stock for product {sale.ProductID} cannot cover recorded sales. Restore the purchase quantity or correct the sales first.");
            }
        }
        return allocations;
    }
}
