using AutoMapper;
using MediatR;
using PerfumeStore.Application.PaymentVouchers.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.PaymentVouchers.Queries.GetAllPaymentVouchers;

public class GetAllPaymentVouchersQueryHandler(IPaymentVouchersRepository paymentVouchersRepository, IMapper mapper) : IRequestHandler<GetAllPaymentVouchersQuery, IEnumerable<PaymentVoucherDto>> {
    public async Task<IEnumerable<PaymentVoucherDto>> Handle(GetAllPaymentVouchersQuery request, CancellationToken cancellationToken) {
        var items = await paymentVouchersRepository.GetAllAsync(request.Search, request.FromDate, request.ToDate, request.Filters);
        return mapper.Map<IEnumerable<PaymentVoucherDto>>(items);
    }
}
