using PerfumeStore.Application.PurchaseInvoiceItems.Dtos;

namespace PerfumeStore.Application.PurchaseInvoices.Dtos {
    public class PurchaseInvoiceDetailsDto {
        public long ID { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public string? SupplierName { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal? DebtAmount { get; set; }

        public Guid? SupplierId { get; set; }
        public int? MoneyAccountId { get; set; }
        public string? DebtNotes { get; set; }
        public decimal AppliedVoucherPayments { get; set; }
        public decimal OriginalDebtAmount => Math.Max(0m, (DebtAmount ?? 0m) + AppliedVoucherPayments - CreditRefundDue);
        public decimal CreditRefundDue => ItemsTotal.HasValue ? Math.Max(0m, AmountPaid + AppliedVoucherPayments - ItemsTotal.Value) : 0m;
        public string DebtStatus => CreditRefundDue > 0 ? "Credit / refund due" : (DebtAmount ?? 0m) > 0 ? (AppliedVoucherPayments > 0 ? "Partially paid" : "Unpaid") : "Settled";

        public int ProductsCount { get; set; }

        public decimal? ItemsTotal => Products.Any(p => p.UnitCost == null) ? null : Products.Sum(p => p.LineTotal);

        public List<PurchaseInvoiceItemDetailsDto> Products { get; set; } = [];
    }
}
