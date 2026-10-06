namespace PerfumeStore.Domain.Entities;

public class Product : BaseEntity {
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public int? MinStock { get; set; }

    public int? BrandID { get; set; }
    public int ProductCategoryID { get; set; }

    public Brand? Brand { get; set; }
    public Inventory? Inventory { get; set; }
    public ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
    public ProductCategory ProductCategory { get; set; } = null!;

    public ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; private set; } = new List<PurchaseInvoiceItem>();
    public ICollection<SalesInvoiceItem> SalesInvoiceItems { get; private set; } = new List<SalesInvoiceItem>();
}
