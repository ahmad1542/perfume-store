namespace PerfumeStore.Domain.Entities;

public class SalesInvoiceItem {
    public long ID { get; set; }

    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public bool IsPriceEstimated { get; set; }
    public bool UsesFifoCost { get; set; }
    public decimal? CostTotal { get; set; }
    public bool IsCostEstimated { get; set; }

    public long SalesInvoiceID { get; set; }

    public int ProductID { get; set; }

    public Product Product { get; set; } = null!;

    public SalesInvoice SalesInvoice { get; set; } = null!;
}
