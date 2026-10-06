using MediatR;

namespace PerfumeStore.Application.Inventory.Commands.CreateInventory {
    public class CreateInventoryCommand : IRequest<int> {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal? AddedUnitCost { get; set; }
    }
}
