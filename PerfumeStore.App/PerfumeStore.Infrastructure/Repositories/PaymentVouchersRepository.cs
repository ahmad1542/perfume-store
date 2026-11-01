using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories;

public class PaymentVouchersRepository(PerfumeStoreDbContext dbContext) : IPaymentVouchersRepository {
    public async Task<long> AddAsync(PaymentVoucher paymentVoucher, IEnumerable<PaymentVoucherPurchaseInvoice> applications) {
        var applicationList = applications.ToList();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try {
            await dbContext.PaymentVouchers.AddAsync(paymentVoucher);
            await dbContext.SaveChangesAsync();

            foreach (var application in applicationList) {
                var invoice = await dbContext.PurchaseInvoices
                    .Include(x => x.Debt)
                    .FirstOrDefaultAsync(x => x.ID == application.PurchaseInvoiceId);

                if (invoice == null) {
                    throw new Exception($"Purchase invoice #{application.PurchaseInvoiceId} was not found.");
                }

                if (invoice.SupplierId != paymentVoucher.SupplierId) {
                    throw new Exception($"Purchase invoice #{application.PurchaseInvoiceId} does not belong to the selected supplier.");
                }

                if (invoice.Debt == null || invoice.Debt.IsDeleted || invoice.Debt.Amount <= 0) {
                    throw new Exception($"Purchase invoice #{application.PurchaseInvoiceId} has no remaining debt.");
                }

                if (application.AppliedAmount > invoice.Debt.Amount) {
                    throw new Exception($"Applied amount for purchase invoice #{application.PurchaseInvoiceId} exceeds its remaining debt.");
                }

                application.PaymentVoucherId = paymentVoucher.ID;
                await dbContext.PaymentVoucherPurchaseInvoices.AddAsync(application);

                invoice.Debt.Amount -= application.AppliedAmount;
                if (invoice.Debt.Amount <= 0) {
                    invoice.Debt.Amount = 0;
                    invoice.Debt.IsDeleted = true;
                }
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return paymentVoucher.ID;
        } catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<PaymentVoucher>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null) {
        IQueryable<PaymentVoucher> query = dbContext.PaymentVouchers
            .Include(x => x.Supplier)
            .Include(x => x.MoneyAccount)
            .Include(x => x.AppliedPurchaseInvoices);

        if (!string.IsNullOrWhiteSpace(search)) {
            search = search.Trim();
            var isLong = long.TryParse(search, out var searchId);
            var isDecimal = decimal.TryParse(search, out var searchAmount);

            query = query.Where(x =>
                (isLong && x.ID == searchId) ||
                (isDecimal && x.Amount == searchAmount) ||
                (x.Supplier.Name.Contains(search)) ||
                (x.Notes != null && x.Notes.Contains(search)) ||
                x.AppliedPurchaseInvoices.Any(a => a.PurchaseInvoiceId.ToString().Contains(search))
            );
        }

        return await query.Filter(filters).WithinDates(fromDate, toDate).OrderByDescending(x => x.Date).ThenByDescending(x => x.ID).ToListAsync();
    }

    public async Task<PaymentVoucher?> GetByIdAsync(long id) {
        return await dbContext.PaymentVouchers
            .Include(x => x.Supplier)
            .Include(x => x.MoneyAccount)
            .Include(x => x.AppliedPurchaseInvoices)
                .ThenInclude(x => x.PurchaseInvoice)
            .FirstOrDefaultAsync(x => x.ID == id);
    }

    public async Task<IEnumerable<PurchaseInvoice>> GetOpenPurchaseInvoicesAsync(Guid supplierId) {
        return await dbContext.PurchaseInvoices
            .Include(x => x.Debt)
            .Where(x => x.SupplierId == supplierId && x.Debt != null && !x.Debt.IsDeleted && x.Debt.Amount > 0)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.ID)
            .ToListAsync();
    }
}
