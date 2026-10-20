namespace PerfumeStore.Application.FinancialHistory;

public interface IFinancialHistoryService {
    Task<FinancialHistoryDto> GetAsync(string kind, int year, int month, int? accountId, CancellationToken ct = default);
}

public class FinancialHistoryDto {
    public string Kind { get; set; } = "";
    public string MonthLabel { get; set; } = "";
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal MoneyIn { get; set; }
    public decimal MoneyOut { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal CurrentAccountBalance { get; set; }
    public decimal BalanceDifference { get; set; }
    public DateTime? TrackingStartedAt { get; set; }
    public List<FinancialHistoryRowDto> Rows { get; set; } = [];
}

public class FinancialHistoryRowDto {
    public bool IsSourceDeleted { get; set; }
    public DateTime Date { get; set; }
    public DateTime RecordedAt { get; set; }
    public string SourceType { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string Description { get; set; } = "";
    public string? AccountName { get; set; }
    public decimal CashChange { get; set; }
    public decimal RevenueChange { get; set; }
    public decimal CostChange { get; set; }
    public decimal ExpenseChange { get; set; }
    public decimal ProfitChange => RevenueChange - CostChange - ExpenseChange;
    public decimal RunningBalance { get; set; }
    public bool IsReconstructed { get; set; }
    public bool IsEstimated { get; set; }
}
