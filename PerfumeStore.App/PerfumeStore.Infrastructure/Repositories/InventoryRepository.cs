using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories {
    public class InventoryRepository(PerfumeStoreDbContext dbContext) : IInventoryRepository {
        public async Task<int> AddAsync(Inventory inventory, decimal? addedUnitCost = null) {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            AddStockBatch(inventory.ProductID, inventory.Quantity, addedUnitCost);
            await dbContext.Inventory.AddAsync(inventory);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return inventory.ProductID;
        }

        public async Task UpdateQuantityAsync(int productId, int quantity, decimal? addedUnitCost) {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            var inventory = await dbContext.Inventory.FindAsync(productId)
                ?? throw new NotFoundException(nameof(Inventory), productId.ToString());
            var ledger = new InventoryCostLedger(dbContext);
            await ledger.SynchronizeAsync(productId, inventory.Quantity);
            var added = quantity - inventory.Quantity;
            if (added > 0) AddStockBatch(productId, added, addedUnitCost);
            inventory.Quantity = quantity;
            await dbContext.SaveChangesAsync();
            await ledger.SynchronizeAsync(productId, quantity);
            await ledger.RecalculateAsync(productId);
            await transaction.CommitAsync();
        }

        private void AddStockBatch(int productId, int quantity, decimal? cost) {
            if (quantity <= 0) return;
            if (!cost.HasValue)
                throw new BusinessRuleException("Enter the cost per unit for the stock being added.");
            var unitCost = InvoicePrices.Resolve(productId, cost.Value, null);
            dbContext.InventoryBatches.Add(new InventoryBatch {
                ProductID = productId, OriginalQuantity = quantity, RemainingQuantity = quantity,
                UnitCost = unitCost, ReceivedAt = DateTime.Now, Source = "Manual addition"
            });
        }

        public async Task<IEnumerable<Inventory>> GetAllAsync(string? search = null) {
            IQueryable<Inventory> query = dbContext.Inventory.Include(c => c.Product).ThenInclude(p => p.InventoryBatches);
            if (!string.IsNullOrWhiteSpace(search)) {
                query = query.Where(c => c.Product.Name.Contains(search));
            }
            return await query.ToListAsync();
        }

        public async Task<Inventory?> GetByIdAsync(int id) {
            return await dbContext.Inventory
                                .Include(c => c.Product).ThenInclude(p => p.InventoryBatches)
                                .FirstOrDefaultAsync(c => c.ProductID == id);
        }

        public async Task SaveChangesAsync() => await dbContext.SaveChangesAsync();
    }
}
