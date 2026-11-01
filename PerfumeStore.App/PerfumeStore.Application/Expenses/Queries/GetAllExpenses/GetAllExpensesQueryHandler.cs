using AutoMapper;
using MediatR;
using PerfumeStore.Application.Expenses.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Expenses.Queries.GetAllExpenses {
    public class GetAllExpensesQueryHandler(IExpensesRepository expensesRepository, IMapper mapper) : IRequestHandler<GetAllExpensesQuery, IEnumerable<ExpenseDto>> {
        public async Task<IEnumerable<ExpenseDto>> Handle(GetAllExpensesQuery request, CancellationToken cancellationToken) {
            var expenses = await expensesRepository.GetAllAsync(request.Search, request.FromDate, request.ToDate, request.ExpenseTypeIds, request.Filters);
            var expenseDtos = mapper.Map<IEnumerable<ExpenseDto>>(expenses);
            return expenseDtos;
        }
    }
}
