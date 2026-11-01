using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.SalesInvoices.Dtos;

namespace PerfumeStore.Application.SalesInvoices.Queries.GetAllSalesinvoices {
    public class GetAllSalesinvoicesQuery : IRequest<IEnumerable<SalesInvoiceDto>> {
        public string? Search { get; set; }
        public RecordFilters Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
