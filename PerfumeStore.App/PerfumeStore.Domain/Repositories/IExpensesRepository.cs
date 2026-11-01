using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories {
    public interface IExpensesRepository {
        Task<Expense?> GetByIdAsync(long id);
        Task<IEnumerable<Expense>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, IEnumerable<long>? expenseTypeIds = null, RecordFilters? filters = null);
        Task<long> AddAsync(Expense expense);
        Task DeleteAsync(Expense expense);
        Task SaveChangesAsync();
    }
}
