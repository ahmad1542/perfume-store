using AutoMapper;
using MediatR;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Inventory.Commands.UpdateInventory {
    public class UpdateInventoryCommandHandler(IInventoryRepository inventoryRepository) : IRequestHandler<UpdateInventoryCommand> {
        public async Task Handle(UpdateInventoryCommand request, CancellationToken cancellationToken) {
            var inventory = await inventoryRepository.GetByIdAsync(request.ProductID);
            if (inventory == null) 
                throw new NotFoundException(nameof(Domain.Entities.Inventory), request.ProductID.ToString());
            await inventoryRepository.UpdateQuantityAsync(request.ProductID, request.Quantity, request.AddedUnitCost);
        }
    }
}
