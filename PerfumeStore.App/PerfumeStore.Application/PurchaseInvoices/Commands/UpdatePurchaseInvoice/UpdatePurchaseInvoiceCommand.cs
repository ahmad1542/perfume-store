using MediatR;

namespace PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice {
    public class UpdatePurchaseInvoiceCommand : IRequest {
        public long ID { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        public Guid SupplierId { get; set; }

        public decimal AmountPaid { get; set; }
        public int? MoneyAccountId { get; set; }

        public decimal? DebtAmount { get; set; }
        public string? DebtNotes { get; set; }
        public bool HasDebt { get; set; } = false;
        public Dictionary<int, decimal>? UnitCosts { get; set; }
        public Dictionary<int, int> Products { get; set; } = [];
    }
}
