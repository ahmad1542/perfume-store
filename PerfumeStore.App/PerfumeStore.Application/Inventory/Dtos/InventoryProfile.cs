using AutoMapper;
using PerfumeStore.Application.Inventory.Commands.CreateInventory;
using PerfumeStore.Application.Inventory.Commands.UpdateInventory;
using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Application.Inventory.Dtos {
    public class InventoryProfile : Profile {
        public InventoryProfile() {
            CreateMap<InventoryBatch, InventoryBatchDto>();
            CreateMap<Domain.Entities.Inventory, InventoryDto>()
                .ForMember(d => d.MinStock, opt => opt.MapFrom(s => s.Product.MinStock))
                .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name))
                .ForMember(d => d.Batches, opt => opt.MapFrom(s => s.Product.InventoryBatches
                    .Where(b => b.RemainingQuantity > 0).OrderBy(b => b.ReceivedAt.Date).ThenBy(b => b.ID)));
             CreateMap<CreateInventoryCommand, Domain.Entities.Inventory>();
             CreateMap<UpdateInventoryCommand, Domain.Entities.Inventory>();
        }
    }
}
