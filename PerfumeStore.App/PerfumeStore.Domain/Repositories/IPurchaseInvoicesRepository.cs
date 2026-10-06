using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories {
    public interface IPurchaseInvoicesRepository {
        Task<PurchaseInvoice?> GetByIdAsync(long id);
        Task<IEnumerable<PurchaseInvoice>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<long> AddAsync(PurchaseInvoice purchaseInvoice, Dictionary<int, int> products, IReadOnlyDictionary<int, decimal>? unitCosts = null);
        Task UpdateProductsAsync(long salesInvoiceId, Dictionary<int, int> products, IReadOnlyDictionary<int, decimal>? unitCosts = null);
        Task SaveChangesAsync();
    }
}
