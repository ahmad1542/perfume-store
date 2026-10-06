using PerfumeStore.Application.PurchaseInvoiceItems.Dtos;

namespace PerfumeStore.Application.PurchaseInvoices.Dtos {
    public class PurchaseInvoiceDetailsDto {
        public long ID { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public string? SupplierName { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal? DebtAmount { get; set; }

        public int ProductsCount { get; set; }

        public decimal? ItemsTotal => Products.Any(p => p.UnitCost == null) ? null : Products.Sum(p => p.LineTotal);

        public List<PurchaseInvoiceItemDetailsDto> Products { get; set; } = [];
    }
}
