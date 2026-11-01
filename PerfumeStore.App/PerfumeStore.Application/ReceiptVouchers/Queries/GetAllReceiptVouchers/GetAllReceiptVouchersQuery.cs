using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.ReceiptVouchers.Dtos;

namespace PerfumeStore.Application.ReceiptVouchers.Queries.GetAllReceiptVouchers;

public class GetAllReceiptVouchersQuery : IRequest<IEnumerable<ReceiptVoucherDto>> {
    public string? Search { get; set; }
    public RecordFilters Filters { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
