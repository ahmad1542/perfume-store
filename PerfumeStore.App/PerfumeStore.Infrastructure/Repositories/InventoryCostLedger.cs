using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories;

internal sealed class InventoryCostLedger(PerfumeStoreDbContext dbContext) {
    // Reconstruct this product's legacy history only when an old receipt needs
    // correction. Prices and any unrecorded stock differences remain estimates.
    public async Task PrepareLegacyHistoryAsync(int productId) {
        var batches = await dbContext.InventoryBatches.Where(b => b.ProductID == productId).ToListAsync();
        var linkedReceipts = batches.Where(b => b.PurchaseInvoiceID.HasValue)
            .Select(b => b.PurchaseInvoiceID!.Value).ToHashSet();
        var receipts = await dbContext.PurchaseInvoiceItems.Include(i => i.PurchaseInvoice)
            .Where(i => i.ProductID == productId).ToListAsync();
        var missing = receipts.Where(i => !linkedReceipts.Contains(i.PurchaseInvoiceID)).ToList();
        if (missing.Count == 0) return;
        var legacySales = await dbContext.SalesInvoiceItems
            .Where(i => i.ProductID == productId && !i.UsesFifoCost).ToListAsync();
        var opening = batches.Where(b => b.PurchaseInvoiceID == null &&
            (b.Source == "Opening stock" || b.Source == "Legacy return")).ToList();
        var required = opening.Sum(b => b.OriginalQuantity) + legacySales.Sum(s => s.Quantity);
        var product = await dbContext.Products.FindAsync(productId);
        var reconstructed = missing.OrderBy(i => i.PurchaseInvoice.Date).ThenBy(i => i.PurchaseInvoiceID)
            .Select(i => new InventoryBatch {
                ProductID = productId, PurchaseInvoiceID = i.PurchaseInvoiceID,
                OriginalQuantity = i.Quantity, UnitCost = i.UnitCost ?? product!.CostPrice,
                ReceivedAt = i.PurchaseInvoice.Date, IsCostEstimated = true, Source = "Estimated purchase"
            }).ToList();
        var excess = reconstructed.Sum(b => b.OriginalQuantity) - required;
        // The old system did not record manual stock losses. Reconcile them
        // against the earliest estimated receipts, preserving today's count.
        foreach (var batch in reconstructed) {
            if (excess <= 0) break;
            var removed = Math.Min(excess, batch.OriginalQuantity);
            batch.OriginalQuantity -= removed;
            excess -= removed;
        }
        foreach (var batch in opening) batch.OriginalQuantity = batch.RemainingQuantity = 0;
        if (excess < 0) dbContext.InventoryBatches.Add(new InventoryBatch {
            ProductID = productId, OriginalQuantity = -excess, UnitCost = product!.CostPrice,
            ReceivedAt = DateTime.MinValue, IsCostEstimated = true, Source = "Opening stock"
        });
        dbContext.InventoryBatches.AddRange(reconstructed);
        foreach (var sale in legacySales) sale.UsesFifoCost = true;
        await dbContext.SaveChangesAsync();
        await RecalculateAsync(productId);
    }
    public async Task SynchronizeAsync(int productId, int quantity) {
        var batches = await dbContext.InventoryBatches.Where(b => b.ProductID == productId)
            .OrderBy(b => b.ReceivedAt).ThenBy(b => b.ID).ToListAsync();
        var difference = quantity - batches.Sum(b => b.RemainingQuantity);
        if (difference > 0) {
            var product = await dbContext.Products.FindAsync(productId);
            dbContext.InventoryBatches.Add(new InventoryBatch {
                ProductID = productId, OriginalQuantity = difference, RemainingQuantity = difference,
                UnitCost = product!.CostPrice, IsCostEstimated = true,
                ReceivedAt = DateTime.Now, Source = "Adjustment"
            });
        } else if (difference < 0) {
            var removed = -difference;
            foreach (var batch in batches.Where(b => b.RemainingQuantity > 0)) {
                var count = Math.Min(removed, batch.RemainingQuantity);
                batch.OriginalQuantity -= count;
                batch.RemainingQuantity -= count;
                removed -= count;
                if (removed == 0) break;
            }
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task RecalculateAsync(int productId) {
        var batches = await dbContext.InventoryBatches.Where(b => b.ProductID == productId).ToListAsync();
        if (batches.Any(b => b.OriginalQuantity < 0))
            throw new BusinessRuleException("This correction exceeds the stock represented by an estimated historical receipt. Correct the inventory count or related invoices first.");
        var sales = await dbContext.SalesInvoiceItems.Where(s => s.ProductID == productId && s.UsesFifoCost).ToListAsync();
        var allocations = await dbContext.SalesCostAllocations
            .Where(a => a.SalesInvoiceItem.ProductID == productId).ToListAsync();
        dbContext.SalesCostAllocations.RemoveRange(allocations);
        dbContext.SalesCostAllocations.AddRange(FifoCosting.Recalculate(batches, sales));
        await dbContext.SaveChangesAsync();
    }

    public async Task RecordPurchaseAsync(long invoiceId, int productId, int quantity, decimal cost, DateTime date, bool estimated) {
        var batch = await dbContext.InventoryBatches
            .SingleOrDefaultAsync(b => b.PurchaseInvoiceID == invoiceId && b.ProductID == productId);
        if (batch == null) {
            batch = new InventoryBatch { ProductID = productId, PurchaseInvoiceID = invoiceId };
            dbContext.InventoryBatches.Add(batch);
        }
        batch.OriginalQuantity = quantity;
        batch.UnitCost = cost;
        batch.ReceivedAt = date;
        batch.IsCostEstimated = estimated || batch.Source == "Estimated purchase";
        await dbContext.SaveChangesAsync();
    }

    public void RestoreLegacySale(SalesInvoiceItem item) {
        if (item.UsesFifoCost || item.Quantity <= 0) return;
        dbContext.InventoryBatches.Add(new InventoryBatch {
            ProductID = item.ProductID, OriginalQuantity = item.Quantity, RemainingQuantity = item.Quantity,
            UnitCost = item.CostTotal / item.Quantity ?? item.Product.CostPrice,
            IsCostEstimated = true, ReceivedAt = DateTime.MinValue, Source = "Legacy return"
        });
    }
}
