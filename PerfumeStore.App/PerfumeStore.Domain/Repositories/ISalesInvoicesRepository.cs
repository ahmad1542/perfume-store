using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories {
    public interface ISalesInvoicesRepository {
        Task<SalesInvoice?> GetByIdAsync(long id);
        Task<IEnumerable<SalesInvoice>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<long> AddAsync(SalesInvoice salesInvoice, Dictionary<int, int> products, IReadOnlyDictionary<int, decimal>? unitPrices = null);
        Task UpdateProductsAsync(long salesInvoiceId, Dictionary<int, int> products, IReadOnlyDictionary<int, decimal>? unitPrices = null);
        Task SaveChangesAsync();
    }
}
