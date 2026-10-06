using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories {
    public class PurchaseInvoicesRepository(PerfumeStoreDbContext dbContext) : IPurchaseInvoicesRepository {
        public async Task<long> AddAsync(PurchaseInvoice purchaseInvoice, Dictionary<int, int> products, IReadOnlyDictionary<int, decimal>? prices = null) {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try {
                var ledger = new InventoryCostLedger(dbContext);
                await dbContext.PurchaseInvoices.AddAsync(purchaseInvoice);
                await dbContext.SaveChangesAsync();

                foreach (var product in products) {
                    var stock = await dbContext.Inventory.FindAsync(product.Key);
                    await ledger.SynchronizeAsync(product.Key, stock?.Quantity ?? 0);
                    var productEntity = await dbContext.Products.FindAsync(product.Key);
                    if (productEntity == null) {
                        throw new NotFoundException(nameof(Product), product.Key.ToString());
                    }

                    await dbContext.PurchaseInvoiceItems.AddAsync(new PurchaseInvoiceItem {
                        PurchaseInvoiceID = purchaseInvoice.ID,
                        ProductID = product.Key,
                        Quantity = product.Value,
                        UnitCost = InvoicePrices.Resolve(product.Key, productEntity.CostPrice, prices)
                    });

                    await IncreaseInventoryAsync(product.Key, product.Value);
                    await ledger.RecordPurchaseAsync(purchaseInvoice.ID, product.Key, product.Value,
                        InvoicePrices.Resolve(product.Key, productEntity.CostPrice, prices), purchaseInvoice.Date, false);
                }

                await dbContext.SaveChangesAsync();
                foreach (var productId in products.Keys) await ledger.RecalculateAsync(productId);
                await transaction.CommitAsync();

                return purchaseInvoice.ID;
            } catch {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<PurchaseInvoice>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null) {
            IQueryable<PurchaseInvoice> query = dbContext.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.Debt)
                .Include(p => p.PurchaseInvoiceItems);

            if (fromDate.HasValue) {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.Date >= from);
            }

            if (toDate.HasValue) {
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.Date <= to);
            }

            if (!string.IsNullOrWhiteSpace(search)) {
                search = search.Trim();

                bool isLong = long.TryParse(search, out var searchId);
                bool isDecimal = decimal.TryParse(search, out var searchAmount);

                query = query.Where(x =>
                    (isLong && x.ID == searchId) ||
                    (isDecimal && x.AmountPaid == searchAmount) ||
                    (x.Supplier != null && x.Supplier.Name.Contains(search)) ||
                    (isDecimal && (
                        (x.Debt != null && x.Debt.Amount == searchAmount) ||
                        (searchAmount == 0 && x.Debt == null)
                    ))
                );
            }

            return await query.ToListAsync();
        }

        public async Task<PurchaseInvoice?> GetByIdAsync(long id) {
            var purchaseInvoice = await dbContext.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.Debt)
                                .Include(p => p.PurchaseInvoiceItems).ThenInclude(i => i.Product).FirstOrDefaultAsync(s => s.ID == id);

            return purchaseInvoice;
        }

        public async Task UpdateProductsAsync(long purchaseInvoiceId, Dictionary<int, int> products, IReadOnlyDictionary<int, decimal>? prices = null) {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try {
                var ledger = new InventoryCostLedger(dbContext);
                var oldItems = await dbContext.PurchaseInvoiceItems
                    .Where(x => x.PurchaseInvoiceID == purchaseInvoiceId)
                    .ToListAsync();

                var affectedProducts = oldItems.Select(i => i.ProductID).Union(products.Keys).ToList();
                foreach (var productId in affectedProducts) {
                    var stock = await dbContext.Inventory.FindAsync(productId);
                    await ledger.SynchronizeAsync(productId, stock?.Quantity ?? 0);
                    await ledger.PrepareLegacyHistoryAsync(productId);
                }
                // Remove old purchased quantities from inventory first
                foreach (var oldItem in oldItems) {
                    await DecreaseInventoryForPurchaseUpdateAsync(oldItem.ProductID, oldItem.Quantity);
                    var oldBatch = await dbContext.InventoryBatches.SingleOrDefaultAsync(b =>
                        b.PurchaseInvoiceID == purchaseInvoiceId && b.ProductID == oldItem.ProductID);
                    if (oldBatch != null) oldBatch.OriginalQuantity -= oldItem.Quantity;
                }

                dbContext.PurchaseInvoiceItems.RemoveRange(oldItems);

                foreach (var product in products) {
                    var productEntity = await dbContext.Products.FindAsync(product.Key);
                    if (productEntity == null) {
                        throw new NotFoundException(nameof(Product), product.Key.ToString());
                    }

                    await dbContext.PurchaseInvoiceItems.AddAsync(new PurchaseInvoiceItem {
                        PurchaseInvoiceID = purchaseInvoiceId,
                        ProductID = product.Key,
                        Quantity = product.Value,
                        UnitCost = InvoicePrices.Resolve(product.Key, productEntity.CostPrice, prices,
                            oldItems.FirstOrDefault(i => i.ProductID == product.Key)?.UnitCost),
                        IsPriceEstimated = InvoicePrices.PreserveEstimate(
                            oldItems.FirstOrDefault(i => i.ProductID == product.Key)?.IsPriceEstimated ?? false,
                            oldItems.FirstOrDefault(i => i.ProductID == product.Key)?.UnitCost,
                            InvoicePrices.Resolve(product.Key, productEntity.CostPrice, prices,
                                oldItems.FirstOrDefault(i => i.ProductID == product.Key)?.UnitCost))
                    });

                    // Apply the new purchase
                    await IncreaseInventoryAsync(product.Key, product.Value);
                    var invoice = await dbContext.PurchaseInvoices.FindAsync(purchaseInvoiceId);
                    var oldItem = oldItems.FirstOrDefault(i => i.ProductID == product.Key);
                    var existingBatch = await dbContext.InventoryBatches.SingleOrDefaultAsync(b =>
                        b.PurchaseInvoiceID == purchaseInvoiceId && b.ProductID == product.Key);
                    await ledger.RecordPurchaseAsync(purchaseInvoiceId, product.Key,
                        product.Value + (existingBatch?.OriginalQuantity ?? 0),
                        InvoicePrices.Resolve(product.Key, productEntity.CostPrice, prices, oldItem?.UnitCost),
                        invoice!.Date, InvoicePrices.PreserveEstimate(oldItem?.IsPriceEstimated ?? false,
                            oldItem?.UnitCost, InvoicePrices.Resolve(product.Key, productEntity.CostPrice, prices, oldItem?.UnitCost)));
                }

                await dbContext.SaveChangesAsync();
                foreach (var productId in affectedProducts) await ledger.RecalculateAsync(productId);
                await transaction.CommitAsync();
            } catch {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task SaveChangesAsync() => await dbContext.SaveChangesAsync();

        private async Task<bool> CheckIfProductExist(int id) {
            return await dbContext.Products.AnyAsync(p => p.ID == id);
        }

        private async Task IncreaseInventoryAsync(int productId, int quantity) {
            var inventory = await dbContext.Inventory
                .FirstOrDefaultAsync(x => x.ProductID == productId);

            if (inventory == null) {
                await dbContext.Inventory.AddAsync(new Inventory {
                    ProductID = productId,
                    Quantity = quantity
                });
            } else {
                inventory.Quantity += quantity;
            }
        }

        private async Task DecreaseInventoryForPurchaseUpdateAsync(int productId, int quantity) {
            var inventory = await dbContext.Inventory
                .FirstOrDefaultAsync(x => x.ProductID == productId);

            if (inventory == null) {
                throw new Exception($"Inventory record not found for product id {productId}.");
            }

            // The old receipt is reversed temporarily; the corrected receipt is applied
            // before FIFO verifies that the final stock covers every recorded sale.
            inventory.Quantity -= quantity;
        }
    }
}
