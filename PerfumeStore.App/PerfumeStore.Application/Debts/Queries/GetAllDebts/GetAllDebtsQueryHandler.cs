using AutoMapper;
using MediatR;
using PerfumeStore.Application.Debts.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Debts.Queries.GetAllDebts {
    public class GetAllDebtsQueryHandler(IDebtsRepository debtsRepository, IMapper mapper) : IRequestHandler<GetAllDebtsQuery, IEnumerable<DebtDto>> {
        public async Task<IEnumerable<DebtDto>> Handle(GetAllDebtsQuery request, CancellationToken cancellationToken) {
            var debts = await debtsRepository.GetAllAsync(request.IncludeSettled, request.Search, request.FromDate, request.ToDate, request.Filters);
            return mapper.Map<IEnumerable<DebtDto>>(debts);
        }
    }
}