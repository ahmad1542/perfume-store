using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories;

public interface IReceiptVouchersRepository {
    Task<long> AddAsync(ReceiptVoucher receiptVoucher, IEnumerable<ReceiptVoucherSalesInvoice> salesApplications, int? debtId = null);
    Task<IEnumerable<ReceiptVoucher>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null);
    Task<ReceiptVoucher?> GetByIdAsync(long id);
    Task<IEnumerable<SalesInvoice>> GetOpenSalesInvoicesAsync(Guid customerId);
    Task<IEnumerable<Debt>> GetOpenPersonDebtsAsync(Guid personId);
}
