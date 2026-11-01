using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.PurchaseInvoices.Dtos;

namespace PerfumeStore.Application.PurchaseInvoices.Queries.GetAllPurchaseinvoices {
    public class GetAllPurchaseinvoicesQuery : IRequest<IEnumerable<PurchaseInvoiceDto>> {
        public string? Search { get; set; }
        public RecordFilters Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
