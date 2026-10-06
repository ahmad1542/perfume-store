using PerfumeStore.Application.SalesInvoiceItems.Dtos;

namespace PerfumeStore.Application.SalesInvoices.Dtos {
    public class SalesInvoiceDetailsDto {
        public long ID { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public string? CustomerName { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal? DebtAmount { get; set; }

        public int ProductsCount { get; set; }

        public decimal? ItemsTotal => Products.Any(p => p.UnitPrice == null) ? null : Products.Sum(p => p.LineTotal);

        public List<SalesInvoiceItemDetailsDto> Products { get; set; } = [];
    }
}
