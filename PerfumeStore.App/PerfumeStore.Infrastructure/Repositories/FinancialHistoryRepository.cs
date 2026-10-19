using Microsoft.EntityFrameworkCore;
using PerfumeStore.Application.FinancialHistory;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories;

public class FinancialHistoryRepository(PerfumeStoreDbContext db) : IFinancialHistoryService {
    public async Task<FinancialHistoryDto> GetAsync(string kind, int year, int month, int? accountId, CancellationToken ct = default) {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var accounts = await db.MoneyAccounts.AsNoTracking().ToListAsync(ct);
        var query = db.FinancialHistoryEntries.AsNoTracking().Where(x => x.Kind == kind);
        if (kind == "cash" && accountId.HasValue) query = query.Where(x => x.MoneyAccountId == accountId);
        var before = kind == "cash" ? await query.Where(x => x.EffectiveDate < start).ToListAsync(ct) : [];
        var entries = await query.Where(x => x.EffectiveDate >= start && x.EffectiveDate < end).ToListAsync(ct);
        var allCash = kind == "cash" ? await query.Select(x => x.CashChange).ToListAsync(ct) : [];
        // Debt collections are visible in profit history with zero profit impact.
        // Their sale's revenue was already recognized on its original sale date.
        if (kind == "profit") entries.AddRange(await db.FinancialHistoryEntries.AsNoTracking()
            .Where(x => x.Kind == "cash" && (x.SourceType == "ReceiptVouchers" || x.SourceType == "PaymentVouchers")
                && x.EffectiveDate >= start && x.EffectiveDate < end).ToListAsync(ct));
        var rows = entries.GroupBy(x => new { x.OperationId, x.Kind, x.MoneyAccountId, x.SourceType, x.SourceId, x.EffectiveDate })
            .Select(g => new {
                Id = g.Min(x => x.ID),
                Row = new FinancialHistoryRowDto {
                    Date = g.Key.EffectiveDate, RecordedAt = g.Max(x => x.RecordedAt),
                    SourceType = g.Key.SourceType, SourceId = g.Key.SourceId,
                    Description = g.Key.Kind == "cash" && kind == "profit" ? "Debt payment: cash changes; profit does not" : g.First().Description,
                    AccountName = accounts.FirstOrDefault(x => x.ID == g.Key.MoneyAccountId)?.AccountName,
                    CashChange = g.Sum(x => x.CashChange), RevenueChange = g.Sum(x => x.RevenueChange),
                    CostChange = g.Sum(x => x.CostChange), ExpenseChange = g.Sum(x => x.ExpenseChange),
                    IsReconstructed = g.Any(x => x.IsReconstructed), IsEstimated = g.OrderByDescending(x => x.ID).First().IsEstimated
                }
            })
            .Where(x => x.Row.CashChange != 0 || x.Row.RevenueChange != 0 || x.Row.CostChange != 0 || x.Row.ExpenseChange != 0)
            .OrderBy(x => x.Row.Date).ThenBy(x => x.Row.RecordedAt).ThenBy(x => x.Id).Select(x => x.Row).ToList();
        var balance = before.Sum(x => x.CashChange);
        var result = new FinancialHistoryDto {
            Kind = kind, MonthLabel = start.ToString("MMMM yyyy"), OpeningBalance = balance,
            CurrentAccountBalance = accounts.Where(x => accountId == null || x.ID == accountId).Sum(x => x.CurrentBalance),
            BalanceDifference = kind == "cash" ? accounts.Where(x => accountId == null || x.ID == accountId).Sum(x => x.CurrentBalance) - allCash.Sum() : 0,
            TrackingStartedAt = (await db.FinancialHistoryStarts.AsNoTracking().FirstOrDefaultAsync(ct))?.RecordedAt,
            Rows = rows,
            MoneyIn = rows.Where(x => x.CashChange > 0).Sum(x => x.CashChange),
            MoneyOut = -rows.Where(x => x.CashChange < 0).Sum(x => x.CashChange),
            Revenue = rows.Sum(x => x.RevenueChange), Cost = rows.Sum(x => x.CostChange), Expenses = rows.Sum(x => x.ExpenseChange)
        };
        foreach (var row in rows) {
            balance += kind == "cash" ? row.CashChange : row.ProfitChange;
            row.RunningBalance = balance;
        }
        result.ClosingBalance = balance;
        result.NetProfit = result.Revenue - result.Cost - result.Expenses;
        return result;
    }
}
