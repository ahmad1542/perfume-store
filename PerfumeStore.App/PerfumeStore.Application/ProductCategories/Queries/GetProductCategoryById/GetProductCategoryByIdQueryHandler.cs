using AutoMapper;
using MediatR;
using PerfumeStore.Application.ProductCategories.Dtos;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.ProductCategories.Queries.GetProductCategoryById {
    public class GetProductCategoryByIdQueryHandler(IProductCategoriesRepository productCategoriesRepository, IMapper mapper) : IRequestHandler<GetProductCategoryByIdQuery, ProductCategoryDto> {
        public async Task<ProductCategoryDto> Handle(GetProductCategoryByIdQuery request, CancellationToken cancellationToken) {
            var productCategory = await productCategoriesRepository.GetByIdAsync(request.ID);
            if (productCategory is null)
                throw new NotFoundException(nameof(ProductCategory), request.ID.ToString());

            var productCategoryDto = mapper.Map<ProductCategoryDto>(productCategory);
            return productCategoryDto;
        }
    }
}
