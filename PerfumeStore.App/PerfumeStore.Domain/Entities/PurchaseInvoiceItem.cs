using System;
using System.Collections.Generic;

namespace PerfumeStore.Domain.Entities;

public class PurchaseInvoiceItem {
    public long ID { get; set; }

    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public bool IsPriceEstimated { get; set; }

    public long PurchaseInvoiceID { get; set; }

    public int ProductID { get; set; }

    public Product Product { get; set; } = null!;

    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
}
