using AutoMapper;
using MediatR;
using PerfumeStore.Application.MoneyTransactions.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.MoneyTransactions.Queries.GetAllMoneyTransactions {
    public class GetAllMoneyTransactionsQueryHandler(IMoneyTransactionsRepository moneyTransactionsRepository, IMapper mapper) : IRequestHandler<GetAllMoneyTransactionsQuery, IEnumerable<MoneyTransactionDto>> {
        public async Task<IEnumerable<MoneyTransactionDto>> Handle(GetAllMoneyTransactionsQuery request, CancellationToken cancellationToken) {
            var moneyTransactions = await moneyTransactionsRepository.GetAllAsync(request.Search, request.FromDate, request.ToDate, request.Filters);
            return mapper.Map<IEnumerable<MoneyTransactionDto>>(moneyTransactions);
        }
    }
}
