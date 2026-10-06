namespace PerfumeStore.Application.PurchaseInvoiceItems.Dtos {
    public class PurchaseInvoiceItemDetailsDto {
        public long ID { get; set; }

        public int ProductID { get; set; }

        public string? ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public bool IsPriceEstimated { get; set; }
        public decimal? LineTotal => UnitCost * Quantity;
    }
}
