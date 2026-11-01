using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories {
    public class MoneyTransactionsRepository(PerfumeStoreDbContext dbContext) : IMoneyTransactionsRepository {
        public async Task<long> AddAsync(MoneyTransaction moneyTransaction) {
            await dbContext.MoneyTransactions.AddAsync(moneyTransaction);
            await dbContext.SaveChangesAsync();
            return moneyTransaction.ID;
        }

        public async Task<IEnumerable<MoneyTransaction>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null) {
            IQueryable<MoneyTransaction> query = dbContext.MoneyTransactions.Include(e => e.FromMoneyAccount).Include(e => e.ToMoneyAccount);
            if (!string.IsNullOrWhiteSpace(search)) {
                query = query.Where(c => c.FromMoneyAccount.AccountName.Contains(search));
            }
            return await query.Filter(filters).WithinDates(fromDate, toDate).ToListAsync();
        }

        public async Task<MoneyTransaction?> GetByIdAsync(long id) {
            return await dbContext.MoneyTransactions.Include(e => e.FromMoneyAccount).Include(e => e.ToMoneyAccount).FirstOrDefaultAsync(c => c.ID == id);
        }

        public async Task SaveChangesAsync() => await dbContext.SaveChangesAsync();
    }
}
