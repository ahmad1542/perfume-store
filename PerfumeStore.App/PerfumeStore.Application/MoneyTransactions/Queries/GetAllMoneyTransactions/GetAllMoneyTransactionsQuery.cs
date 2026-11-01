using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.MoneyTransactions.Dtos;

namespace PerfumeStore.Application.MoneyTransactions.Queries.GetAllMoneyTransactions {
    public class GetAllMoneyTransactionsQuery(string? search) : IRequest<IEnumerable<MoneyTransactionDto>> {
        public string? Search { get; } = search;
        public RecordFilters Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
