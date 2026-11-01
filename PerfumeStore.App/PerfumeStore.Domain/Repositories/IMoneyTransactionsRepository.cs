using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories {
    public interface IMoneyTransactionsRepository {
        Task<MoneyTransaction?> GetByIdAsync(long id);
        Task<IEnumerable<MoneyTransaction>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null);
        Task<long> AddAsync(MoneyTransaction moneyTransaction);
        Task SaveChangesAsync();
    }
}
