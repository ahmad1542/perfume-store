using AutoMapper;
using MediatR;
using PerfumeStore.Application.SalesInvoices.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.SalesInvoices.Queries.GetAllSalesinvoices {
    public class GetAllSalesInvoicesQueryHandler(ISalesInvoicesRepository salesInvoicesRepository, IMapper mapper) : IRequestHandler<GetAllSalesinvoicesQuery, IEnumerable<SalesInvoiceDto>> {

        public async Task<IEnumerable<SalesInvoiceDto>> Handle(GetAllSalesinvoicesQuery request, CancellationToken cancellationToken) {
            var salesInvoices = await salesInvoicesRepository.GetAllAsync(
                request.Search,
                request.FromDate,
                request.ToDate, request.Filters);

            var invoices = salesInvoices.ToList();
            var result = mapper.Map<List<SalesInvoiceDto>>(invoices);
            if (request.Filters.HasLineFilters) {
                for (var index = 0; index < invoices.Count; index++)
                    result[index].MatchingProductsTotal = invoices[index].SalesInvoiceItems
                        .Where(i => request.Filters.Matches(i.Product)).Sum(i => i.Quantity * (i.UnitPrice ?? i.Product.SalePrice));
            }
            return result;
        }
    }
}
