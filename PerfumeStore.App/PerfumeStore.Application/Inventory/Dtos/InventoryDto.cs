namespace PerfumeStore.Application.Inventory.Dtos {
    public class InventoryDto {
        public int ProductID { get; set; }

        public string ProductName { get; set; } = default!;

        public int Quantity { get; set; }
        public int? MinStock { get; set; }
        public bool IsLowStock => MinStock.HasValue && Quantity <= MinStock.Value;
        public List<InventoryBatchDto> Batches { get; set; } = [];
        public decimal StockCost => Batches.Sum(b => b.RemainingQuantity * b.UnitCost);
        public bool HasEstimatedCosts => Batches.Any(b => b.IsCostEstimated);
    }

    public sealed class InventoryBatchDto {
        public long ID { get; set; }
        public long? PurchaseInvoiceID { get; set; }
        public DateTime ReceivedAt { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public bool IsCostEstimated { get; set; }
        public string Source { get; set; } = "";
    }
}
