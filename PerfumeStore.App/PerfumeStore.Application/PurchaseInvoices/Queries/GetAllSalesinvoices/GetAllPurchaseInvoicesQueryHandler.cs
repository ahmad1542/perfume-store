using AutoMapper;
using MediatR;
using PerfumeStore.Application.PurchaseInvoices.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.PurchaseInvoices.Queries.GetAllPurchaseinvoices {
    public class GetAllPurchaseInvoicesQueryHandler(IPurchaseInvoicesRepository purchaseInvoicesRepository, IMapper mapper) : IRequestHandler<GetAllPurchaseinvoicesQuery, IEnumerable<PurchaseInvoiceDto>> {

        public async Task<IEnumerable<PurchaseInvoiceDto>> Handle(GetAllPurchaseinvoicesQuery request, CancellationToken cancellationToken) {
            var purchaseInvoices = await purchaseInvoicesRepository.GetAllAsync(
                request.Search,
                request.FromDate,
                request.ToDate, request.Filters);

            var invoices = purchaseInvoices.ToList();
            var result = mapper.Map<List<PurchaseInvoiceDto>>(invoices);
            if (request.Filters.HasLineFilters) {
                for (var index = 0; index < invoices.Count; index++)
                    result[index].MatchingProductsTotal = invoices[index].PurchaseInvoiceItems
                        .Where(i => request.Filters.Matches(i.Product)).Sum(i => i.Quantity * (i.UnitCost ?? i.Product.CostPrice));
            }
            return result;
        }
    }
}
