using MediatR;
using PerfumeStore.Application.Inventory.Dtos;

namespace PerfumeStore.Application.Inventory.Commands.UpdateInventory {
    public class UpdateInventoryCommand : IRequest {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal? AddedUnitCost { get; set; }

    }
}
