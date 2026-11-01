using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories {
    public interface IDebtsRepository {
        Task<Debt?> GetByIdAsync(int id);
        Task<IEnumerable<Debt>> GetAllAsync(bool includeSettled, string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null);
        Task<int> AddAsync(Debt debt);
        Task SoftDeleteAsync(Debt debt);
        Task SaveChangesAsync();

        Task<bool> CheckIfSalesInvoiceExist(long id);
        Task<bool> CheckIfPurchaseInvoiceExist(long id);
        Task<bool> CheckIfPersonExist(Guid id);
    }
}
