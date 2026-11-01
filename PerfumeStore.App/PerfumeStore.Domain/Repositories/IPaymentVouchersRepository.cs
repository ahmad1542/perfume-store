using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories;

public interface IPaymentVouchersRepository {
    Task<long> AddAsync(PaymentVoucher paymentVoucher, IEnumerable<PaymentVoucherPurchaseInvoice> applications);
    Task<IEnumerable<PaymentVoucher>> GetAllAsync(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, RecordFilters? filters = null);
    Task<PaymentVoucher?> GetByIdAsync(long id);
    Task<IEnumerable<PurchaseInvoice>> GetOpenPurchaseInvoicesAsync(Guid supplierId);
}
