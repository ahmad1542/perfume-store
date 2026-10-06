namespace PerfumeStore.Domain.Entities;

public sealed class SalesCostAllocation {
    public long ID { get; set; }
    public long SalesInvoiceItemID { get; set; }
    public SalesInvoiceItem SalesInvoiceItem { get; set; } = null!;
    public long InventoryBatchID { get; set; }
    public InventoryBatch InventoryBatch { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsCostEstimated { get; set; }
}
