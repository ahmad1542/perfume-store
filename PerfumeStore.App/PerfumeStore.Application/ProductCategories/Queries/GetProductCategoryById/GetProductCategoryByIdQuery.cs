using MediatR;
using PerfumeStore.Application.ProductCategories.Dtos;

namespace PerfumeStore.Application.ProductCategories.Queries.GetProductCategoryById {
    public class GetProductCategoryByIdQuery(int id) : IRequest<ProductCategoryDto> {
        public int ID { get; set; } = id;
    }
}
