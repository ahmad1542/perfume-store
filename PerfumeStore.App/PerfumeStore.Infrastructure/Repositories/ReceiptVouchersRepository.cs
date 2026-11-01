using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories;

public class ReceiptVouchersRepository(PerfumeStoreDbContext dbContext) : IReceiptVouchersRepository {
    public async Task<long> AddAsync(ReceiptVoucher receiptVoucher, IEnumerable<ReceiptVoucherSalesInvoice> salesApplications, int? debtId = null) {
        var salesApplicationList = salesApplications.ToList();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try {
            await dbContext.ReceiptVouchers.AddAsync(receiptVoucher);
            await dbContext.SaveChangesAsync();

            foreach (var application in salesApplicationList) {
                var invoice = await dbContext.SalesInvoices
                    .Include(x => x.Debt)
                    .FirstOrDefaultAsync(x => x.ID == application.SalesInvoiceId);

                if (invoice == null) {
                    throw new Exception($"Sales invoice #{application.SalesInvoiceId} was not found.");
                }

                if (invoice.CustomerId != receiptVoucher.CustomerId) {
                    throw new Exception($"Sales invoice #{application.SalesInvoiceId} does not belong to the selected customer.");
                }

                if (invoice.Debt == null || invoice.Debt.IsDeleted || invoice.Debt.Amount <= 0) {
                    throw new Exception($"Sales invoice #{application.SalesInvoiceId} has no remaining debt.");
                }

                if (application.AppliedAmount > invoice.Debt.Amount) {
                    throw new Exception($"Applied amount for sales invoice #{application.SalesInvoiceId} exceeds its remaining debt.");
                }

                application.ReceiptVoucherId = receiptVoucher.ID;
                await dbContext.ReceiptVoucherSalesInvoices.AddAsync(application);

                invoice.Debt.Amount -= application.AppliedAmount;
                if (invoice.Debt.Amount <= 0) {
                    invoice.Debt.Amount = 0;
                    invoice.Debt.IsDeleted = true;
                }
            }

            if (debtId.HasValue) {
                var debt = await dbContext.Debts
                    .FirstOrDefaultAsync(x => x.Id == debtId.Value);

                if (debt == null)
                    throw new Exception($"Debt #{debtId.Value} not found.");

                if (debt.IsDeleted || debt.Amount <= 0)
                    throw new Exception("Debt already settled.");

                if (receiptVoucher.Amount > debt.Amount)
                    throw new Exception("Amount exceeds debt.");

                debt.Amount -= receiptVoucher.Amount;

                if (debt.Amount <= 0) {
                    debt.Amount = 0;
                    debt.IsDeleted = true;
                }
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return receiptVoucher.ID;
        } catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<ReceiptVoucher>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null) {
        IQueryable<ReceiptVoucher> query = dbContext.ReceiptVouchers
            .Include(x => x.Customer)
            .Include(x => x.Person)
            .Include(x => x.MoneyAccount)
            .Include(x => x.AppliedSalesInvoices);

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim();
            var isLong = long.TryParse(search, out var searchId);
            var isInt = int.TryParse(search, out var searchDebtId);
            var isDecimal = decimal.TryParse(search, out var searchAmount);

            query = query.Where(x =>
                (isLong && x.ID == searchId) ||
                (isDecimal && x.Amount == searchAmount) ||
                (x.Customer != null && x.Customer.Name.Contains(search)) ||
                (x.Person != null && x.Person.Name.Contains(search)) ||
                (x.Notes != null && x.Notes.Contains(search)) ||
                x.AppliedSalesInvoices.Any(a => a.SalesInvoiceId.ToString().Contains(search))
            );
        }

        return await query.Filter(filters).WithinDates(fromDate, toDate).OrderByDescending(x => x.Date).ThenByDescending(x => x.ID).ToListAsync();
    }

    public async Task<ReceiptVoucher?> GetByIdAsync(long id) {
        return await dbContext.ReceiptVouchers
            .Include(x => x.Customer)
            .Include(x => x.Person)
            .Include(x => x.MoneyAccount)
            .Include(x => x.AppliedSalesInvoices)
                .ThenInclude(x => x.SalesInvoice)
            .FirstOrDefaultAsync(x => x.ID == id);
    }

    public async Task<IEnumerable<SalesInvoice>> GetOpenSalesInvoicesAsync(Guid customerId) {
        return await dbContext.SalesInvoices
            .Include(x => x.Debt)
            .Where(x => x.CustomerId == customerId && x.Debt != null && !x.Debt.IsDeleted && x.Debt.Amount > 0)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.ID)
            .ToListAsync();
    }

    public async Task<IEnumerable<Debt>> GetOpenPersonDebtsAsync(Guid personId) {
        return await dbContext.Debts
            .Include(x => x.Person)
            .Where(x => x.PersonId == personId && x.SalesInvoiceId == null && x.PurchaseInvoiceId == null && !x.IsDeleted && x.Amount > 0)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }
}
