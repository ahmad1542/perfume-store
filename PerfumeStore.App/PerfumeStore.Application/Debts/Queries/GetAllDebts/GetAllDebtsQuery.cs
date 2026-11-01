using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.Debts.Dtos;

namespace PerfumeStore.Application.Debts.Queries.GetAllDebts {
    public class GetAllDebtsQuery(string? search, bool includeSettled) : IRequest<IEnumerable<DebtDto>> {
        public string? Search { get; set; } = search;
        public RecordFilters Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IncludeSettled { get; set; } = includeSettled;
    }
}