namespace PerfumeStore.Domain.Entities;

// Append-only financial effects. Old records are reconstructed once, then every
// saved balance/profit change is recorded alongside the underlying write.
public class FinancialHistoryEntry {
    public long ID { get; set; }
    public Guid OperationId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string Kind { get; set; } = "";
    public int? MoneyAccountId { get; set; }
    public string SourceType { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal CashChange { get; set; }
    public decimal RevenueChange { get; set; }
    public decimal CostChange { get; set; }
    public decimal ExpenseChange { get; set; }
    public bool IsReconstructed { get; set; }
    public bool IsEstimated { get; set; }
}

public class FinancialHistoryStart {
    public int ID { get; set; }
    public DateTime RecordedAt { get; set; }
}
