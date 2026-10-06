namespace PerfumeStore.Domain.Entities;

public sealed class InventoryBatch {
    public long ID { get; set; }
    public int ProductID { get; set; }
    public Product Product { get; set; } = null!;
    public long? PurchaseInvoiceID { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public DateTime ReceivedAt { get; set; }
    public int OriginalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsCostEstimated { get; set; }
    public string Source { get; set; } = "Purchase";
}
