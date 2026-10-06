namespace PerfumeStore.Application.SalesInvoiceItems.Dtos {
    public class SalesInvoiceItemDetailsDto {
        public long ID { get; set; }

        public int ProductID { get; set; }

        public string? ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public bool IsPriceEstimated { get; set; }
        public decimal? LineTotal => UnitPrice * Quantity;
        public decimal? CostTotal { get; set; }
        public bool IsCostEstimated { get; set; }
        public decimal? GrossProfit => LineTotal - CostTotal;
    }
}
