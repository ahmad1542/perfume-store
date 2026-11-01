using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories {
    public class DebtsRepository(PerfumeStoreDbContext dbContext) : IDebtsRepository {

        public async Task<int> AddAsync(Debt debt) {
            await dbContext.Debts.AddAsync(debt);
            await dbContext.SaveChangesAsync();
            return debt.Id;
        }

        public async Task<IEnumerable<Debt>> GetAllAsync(bool includeSettled, string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null) {
            IQueryable<Debt> query = dbContext.Debts
                .Include(d => d.Person)
                .Include(d => d.PurchaseInvoice)
                .Include(d => d.SalesInvoice);

            if (!includeSettled && filters?.Status is not ("all" or "settled"))
                query = query.Where(d => !d.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search)) {
                search = search.Trim();

                query = query.Where(d =>
                    d.Id.ToString().Contains(search) ||
                    d.Amount.ToString().Contains(search) ||
                    (d.Notes != null && d.Notes.Contains(search)) ||
                    (d.SalesInvoice != null && d.SalesInvoice.ID.ToString().Contains(search)) ||
                    (d.PurchaseInvoice != null && d.PurchaseInvoice.ID.ToString().Contains(search)) ||
                    (d.Person != null && d.Person.Name.Contains(search)) ||
                    (d.Person != null && d.Person.Phone.Contains(search))
                );
            }

            return await query.Filter(filters, dbContext).WithinDates(fromDate, toDate).ToListAsync();
        }

        public async Task<Debt?> GetByIdAsync(int id) {
            var debt = await dbContext.Debts
                .Include(d => d.Person)
                .Include(d => d.SalesInvoice)
                .Include(d => d.PurchaseInvoice)
                .FirstOrDefaultAsync(d => d.Id == id);

            return debt;
        }

        public async Task SoftDeleteAsync(Debt debt) {
            debt.IsDeleted = true;
            await dbContext.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() => await dbContext.SaveChangesAsync();

        public async Task<bool> CheckIfSalesInvoiceExist(long id) {
            return await dbContext.SalesInvoices.AnyAsync(x => x.ID == id);
        }

        public async Task<bool> CheckIfPurchaseInvoiceExist(long id) {
            return await dbContext.PurchaseInvoices.AnyAsync(x => x.ID == id);
        }

        public async Task<bool> CheckIfPersonExist(Guid id) {
            return await dbContext.Persons.AnyAsync(x => x.Id == id);
        }
    }
}