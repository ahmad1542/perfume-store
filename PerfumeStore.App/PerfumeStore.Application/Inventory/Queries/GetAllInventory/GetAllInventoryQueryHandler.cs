using AutoMapper;
using MediatR;
using PerfumeStore.Application.Inventory.Dtos;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Inventory.Queries.GetAllInventory {
    public class GetAllInventoryQueryHandler(IInventoryRepository inventory, IMapper mapper) : IRequestHandler<GetAllInventoryQuery, IEnumerable<InventoryDto>> {
        public async Task<IEnumerable<InventoryDto>> Handle(GetAllInventoryQuery request, CancellationToken cancellationToken) {
            var inventories = await inventory.GetAllAsync(request.Search, request.Filters);
            return mapper.Map<IEnumerable<InventoryDto>>(inventories);
        }
    }
}
