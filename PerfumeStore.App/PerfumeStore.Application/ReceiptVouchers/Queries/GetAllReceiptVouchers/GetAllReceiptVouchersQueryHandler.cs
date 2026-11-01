using AutoMapper;
using MediatR;
using PerfumeStore.Application.ReceiptVouchers.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.ReceiptVouchers.Queries.GetAllReceiptVouchers;

public class GetAllReceiptVouchersQueryHandler(IReceiptVouchersRepository receiptVouchersRepository, IMapper mapper) : IRequestHandler<GetAllReceiptVouchersQuery, IEnumerable<ReceiptVoucherDto>> {
    public async Task<IEnumerable<ReceiptVoucherDto>> Handle(GetAllReceiptVouchersQuery request, CancellationToken cancellationToken) {
        var items = await receiptVouchersRepository.GetAllAsync(request.Search, request.FromDate, request.ToDate, request.Filters);
        return mapper.Map<IEnumerable<ReceiptVoucherDto>>(items);
    }
}
