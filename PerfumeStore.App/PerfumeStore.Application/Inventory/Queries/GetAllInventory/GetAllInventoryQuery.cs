using PerfumeStore.Domain.Repositories;
using MediatR;
using PerfumeStore.Application.Inventory.Dtos;

namespace PerfumeStore.Application.Inventory.Queries.GetAllInventory {
    public class GetAllInventoryQuery(string? search) : IRequest<IEnumerable<InventoryDto>> {
        public string? Search { get; set; } = search;
        public RecordFilters Filters { get; set; } = new();
    }
}
