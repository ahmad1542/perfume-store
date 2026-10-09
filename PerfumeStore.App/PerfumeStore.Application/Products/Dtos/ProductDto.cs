namespace PerfumeStore.Application.Products.Dtos {
    public class ProductDto {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public int Capacity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public int? MinStock { get; set; }
        public int? BrandID { get; set; }
        public int ProductCategoryID { get; set; }

        public string CategoryName { get; set; } = null!;

        public string? BrandName { get; set; }
    }
}
