using AutoMapper;
using MediatR;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Inventory.Commands.CreateInventory {
    public class CreateInventoryCommandHandler(IInventoryRepository inventoryRepository, IMapper mapper) : IRequestHandler<CreateInventoryCommand, int> {
        public async Task<int> Handle(CreateInventoryCommand request, CancellationToken cancellationToken) {
            var inventory = mapper.Map<Domain.Entities.Inventory>(request);
            return await inventoryRepository.AddAsync(inventory, request.AddedUnitCost);
        }
    }
}
