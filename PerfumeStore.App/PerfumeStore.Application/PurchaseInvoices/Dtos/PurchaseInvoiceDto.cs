namespace PerfumeStore.Application.PurchaseInvoices.Dtos {
    public class PurchaseInvoiceDto {
        public long ID { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public string? SupplierName { get; set; }

        public decimal AmountPaid { get; set; }
        public decimal InvoiceTotal { get; set; }
        public decimal? MatchingProductsTotal { get; set; }

        public decimal? DebtAmount { get; set; }

        public int ProductsCount { get; set; }
    }
}
