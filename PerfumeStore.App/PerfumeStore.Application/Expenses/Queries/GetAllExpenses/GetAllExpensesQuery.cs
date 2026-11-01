using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.Expenses.Dtos;

namespace PerfumeStore.Application.Expenses.Queries.GetAllExpenses {
    public class GetAllExpensesQuery() : IRequest<IEnumerable<ExpenseDto>> {
        public string? Search { get; set; }
        public RecordFilters Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public IEnumerable<long>? ExpenseTypeIds { get; set; }
    }
}
