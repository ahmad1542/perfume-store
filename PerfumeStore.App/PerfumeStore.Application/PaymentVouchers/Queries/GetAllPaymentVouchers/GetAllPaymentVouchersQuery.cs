using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.PaymentVouchers.Dtos;

namespace PerfumeStore.Application.PaymentVouchers.Queries.GetAllPaymentVouchers;

public class GetAllPaymentVouchersQuery : IRequest<IEnumerable<PaymentVoucherDto>> {
    public string? Search { get; set; }
    public RecordFilters Filters { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
