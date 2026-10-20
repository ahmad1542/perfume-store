using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories {
    public class ExpensesRepository(PerfumeStoreDbContext dbContext) : IExpensesRepository {

        public async Task<long> AddAsync(Expense expense) {
            await dbContext.Expenses.AddAsync(expense);
            await dbContext.SaveChangesAsync();
            return expense.ID;
        }

        public async Task<IEnumerable<Expense>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, IEnumerable<long>? expenseTypeIds = null) {
            IQueryable<Expense> query = dbContext.Expenses
                .AsNoTracking()
                .Include(e => e.ExpenseType)
                .Include(e => e.MoneyAccount);

            if (fromDate.HasValue) {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.Date >= from);
            }

            if (toDate.HasValue) {
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.Date <= to);
            }

            if (expenseTypeIds != null) {
                var typeIds = expenseTypeIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (typeIds.Count > 0) {
                    query = query.Where(x => typeIds.Contains(x.ExpenseTypeId));
                }
            }

            if (!string.IsNullOrWhiteSpace(search)) {
                search = search.Trim();

                bool isLong = long.TryParse(search, out var searchId);
                bool isDecimal = decimal.TryParse(search, out var searchAmount);

                query = query.Where(x =>
                    (isLong && x.ID == searchId) ||
                    (isDecimal && x.Amount == searchAmount) ||
                    x.ExpenseType.Name.Contains(search) ||
                    x.MoneyAccount.AccountName.Contains(search) ||
                    (x.Notes != null && x.Notes.Contains(search))
                );
            }

            query = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.ID);
            return await query.ToListAsync();
        }

        public async Task<Expense?> GetByIdAsync(long id) {
            return await dbContext.Expenses
                .Include(e => e.ExpenseType)
                .Include(e => e.MoneyAccount)
                .FirstOrDefaultAsync(e => e.ID == id);
        }

        public async Task SaveChangesAsync() => await dbContext.SaveChangesAsync();

        public async Task DeleteAsync(Expense expense) {
            dbContext.Expenses.Remove(expense);
            await dbContext.SaveChangesAsync();
        }
    }
}
