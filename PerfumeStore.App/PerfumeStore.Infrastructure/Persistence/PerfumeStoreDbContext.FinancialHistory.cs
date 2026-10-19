using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Infrastructure.Persistence;

public partial class PerfumeStoreDbContext {
    public DbSet<FinancialHistoryEntry> FinancialHistoryEntries { get; set; }
    public DbSet<FinancialHistoryStart> FinancialHistoryStarts { get; set; }
    public FinancialHistoryContext HistoryContext { get; set; } = new();

    private record ProfitState(string Type, string Id, DateTime Date, decimal Revenue, decimal Cost, decimal Expense, bool Estimated);

    private async Task<List<ProfitState>> ReadProfitStateAsync(CancellationToken ct) {
        var items = await SalesInvoiceItems.AsNoTracking().Include(x => x.Product).Include(x => x.SalesInvoice).ToListAsync(ct);
        var expenses = await Expenses.AsNoTracking().ToListAsync(ct);
        return items.GroupBy(x => new { x.SalesInvoiceID, x.SalesInvoice.Date })
            .Select(g => new ProfitState("SalesInvoices", g.Key.SalesInvoiceID.ToString(), g.Key.Date.Date,
                g.Sum(x => x.Quantity * (x.UnitPrice ?? x.Product.SalePrice)),
                g.Sum(x => x.CostTotal ?? x.Quantity * x.Product.CostPrice), 0,
                g.Any(x => x.IsCostEstimated || x.IsPriceEstimated || x.UnitPrice == null || x.CostTotal == null)))
            .Concat(expenses.Select(x => new ProfitState("Expenses", x.ID.ToString(), x.Date.Date, 0, 0, x.Amount, false))).ToList();
    }

    private (string Type, string Id, string Description) CashSource() {
        string type = HistoryContext.Resource;
        object? source = ChangeTracker.Entries().Select(x => x.Entity).LastOrDefault(x =>
            string.Equals(x.GetType().Name + "s", type, StringComparison.OrdinalIgnoreCase));
        // No HTTP request in tests/background work: still retain a useful source.
        source ??= ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified)
            .Select(x => x.Entity).FirstOrDefault(x => x is ReceiptVoucher or PaymentVoucher or Expense or SalesInvoice or PurchaseInvoice or MoneyTransaction or Debt);
        string id = HistoryContext.SourceId;
        if (source != null) {
            type = source switch { Debt => "Debts", _ => source.GetType().Name + "s" };
            id = (source.GetType().GetProperty("ID") ?? source.GetType().GetProperty("Id"))?.GetValue(source)?.ToString() ?? id;
        }
        if (string.IsNullOrEmpty(type)) type = "MoneyAccounts";
        return (type, id, $"{HistoryContext.Action}: {type} {(string.IsNullOrEmpty(id) ? "" : "#" + id)}".Trim());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        ChangeTracker.DetectChanges();
        var cash = ChangeTracker.Entries<MoneyAccount>()
            .Where(x => x.State == EntityState.Added || (x.State == EntityState.Modified && x.Property(a => a.CurrentBalance).IsModified))
            .Select(x => (Entity: x.Entity, Added: x.State == EntityState.Added,
                Delta: x.Entity.CurrentBalance - (x.State == EntityState.Added ? 0m : x.Property(a => a.CurrentBalance).OriginalValue)))
            .Where(x => x.Added || x.Delta != 0).ToList();
        bool profitChanged = ChangeTracker.Entries().Any(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && x.Entity is SalesInvoice or SalesInvoiceItem or Expense or Product);
        if (cash.Count == 0 && !profitChanged) return await base.SaveChangesAsync(cancellationToken);

        await using var transaction = Database.CurrentTransaction == null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var before = profitChanged ? await ReadProfitStateAsync(cancellationToken) : [];
        var count = await base.SaveChangesAsync(cancellationToken);
        var source = CashSource();
        var now = DateTime.UtcNow;
        // Without an HTTP scope use a fresh id for each saved operation.
        var operation = string.IsNullOrEmpty(HistoryContext.Resource) ? Guid.NewGuid() : HistoryContext.OperationId;
        foreach (var change in cash) {
            FinancialHistoryEntries.Add(new FinancialHistoryEntry {
                Kind = "cash", OperationId = operation, RecordedAt = now,
                EffectiveDate = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem")).Date,
                MoneyAccountId = change.Entity.ID, CashChange = change.Delta,
                SourceType = change.Added ? "MoneyAccounts" : source.Type,
                SourceId = change.Added ? change.Entity.ID.ToString() : source.Id,
                Description = change.Added ? $"Opening balance: {change.Entity.AccountName}" : source.Description
            });
        }
        if (profitChanged) {
            var after = await ReadProfitStateAsync(cancellationToken);
            var oldValues = before.ToDictionary(x => (x.Type, x.Id, x.Date));
            var newValues = after.ToDictionary(x => (x.Type, x.Id, x.Date));
            foreach (var key in oldValues.Keys.Union(newValues.Keys)) {
                oldValues.TryGetValue(key, out var old);
                newValues.TryGetValue(key, out var updated);
                decimal revenue = (updated?.Revenue ?? 0) - (old?.Revenue ?? 0);
                decimal cost = (updated?.Cost ?? 0) - (old?.Cost ?? 0);
                decimal expense = (updated?.Expense ?? 0) - (old?.Expense ?? 0);
                if (revenue == 0 && cost == 0 && expense == 0 && old?.Estimated == updated?.Estimated) continue;
                FinancialHistoryEntries.Add(new FinancialHistoryEntry {
                    Kind = "profit", OperationId = operation, RecordedAt = now, EffectiveDate = key.Date,
                    SourceType = key.Type, SourceId = key.Id,
                    Description = string.IsNullOrEmpty(HistoryContext.Resource) ? $"Change: {key.Type} #{key.Id}" : source.Description,
                    RevenueChange = revenue, CostChange = cost, ExpenseChange = expense,
                    IsEstimated = updated?.Estimated ?? old?.Estimated ?? false
                });
            }
        }
        await base.SaveChangesAsync(cancellationToken);
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
        return count;
    }

    // Run once after migration. Historical entries describe today's saved records,
    // not an invented audit trail of unknown historical edits or balances.
    public async Task InitializeFinancialHistoryAsync(CancellationToken ct = default) {
        if (await FinancialHistoryStarts.AnyAsync(ct)) return;
        await using var tx = await Database.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        void Cash(string type, string id, DateTime date, int? account, decimal amount, string description) {
            if (account == null || amount == 0) return;
            FinancialHistoryEntries.Add(new FinancialHistoryEntry {
                Kind = "cash", OperationId = Guid.NewGuid(), RecordedAt = date.Date, EffectiveDate = date.Date,
                SourceType = type, SourceId = id, MoneyAccountId = account, CashChange = amount,
                Description = description, IsReconstructed = true
            });
        }
        foreach (var x in await SalesInvoices.AsNoTracking().ToListAsync(ct)) Cash("SalesInvoices", x.ID.ToString(), x.Date, x.MoneyAccountId, x.AmountPaid, "Sale payment");
        foreach (var x in await PurchaseInvoices.AsNoTracking().ToListAsync(ct)) Cash("PurchaseInvoices", x.ID.ToString(), x.Date, x.MoneyAccountId, -x.AmountPaid, "Purchase payment");
        foreach (var x in await ReceiptVouchers.AsNoTracking().ToListAsync(ct)) Cash("ReceiptVouchers", x.ID.ToString(), x.Date, x.MoneyAccountID, x.Amount, "Debt collected (no new profit)");
        foreach (var x in await PaymentVouchers.AsNoTracking().ToListAsync(ct)) Cash("PaymentVouchers", x.ID.ToString(), x.Date, x.MoneyAccountID, -x.Amount, "Supplier debt paid (no new expense)");
        foreach (var x in await Expenses.AsNoTracking().ToListAsync(ct)) Cash("Expenses", x.ID.ToString(), x.Date, x.MoneyAccountID, -x.Amount, "Expense paid");
        foreach (var x in await MoneyTransactions.AsNoTracking().ToListAsync(ct)) {
            Cash("MoneyTransactions", x.ID.ToString(), x.Date, x.FromMoneyAccountID, -x.TransferAmount, "Transfer out");
            Cash("MoneyTransactions", x.ID.ToString(), x.Date, x.ToMoneyAccountID, x.TransferAmount, "Transfer in");
        }
        foreach (var x in await Debts.AsNoTracking().Where(x => x.SalesInvoiceId == null && x.PurchaseInvoiceId == null).ToListAsync(ct))
            Cash("Debts", x.Id.ToString(), x.Date, x.MoneyAccountId, x.Direction == DebtDirection.Receivable ? -x.Amount : x.Amount, "Standalone debt (remaining amount; reconstructed)");
        var entries = FinancialHistoryEntries.Local.ToList();
        foreach (var account in await MoneyAccounts.AsNoTracking().ToListAsync(ct)) {
            FinancialHistoryEntries.Add(new FinancialHistoryEntry {
                Kind = "cash", OperationId = Guid.NewGuid(), RecordedAt = new DateTime(1900, 1, 1), EffectiveDate = new DateTime(1900, 1, 1),
                MoneyAccountId = account.ID, SourceType = "MoneyAccounts", SourceId = account.ID.ToString(),
                CashChange = account.CurrentBalance - entries.Where(x => x.MoneyAccountId == account.ID).Sum(x => x.CashChange),
                Description = "Reconciled opening balance / untracked earlier adjustments", IsReconstructed = true, IsEstimated = true
            });
        }
        foreach (var state in await ReadProfitStateAsync(ct)) {
            FinancialHistoryEntries.Add(new FinancialHistoryEntry {
                Kind = "profit", OperationId = Guid.NewGuid(), RecordedAt = state.Date, EffectiveDate = state.Date,
                SourceType = state.Type, SourceId = state.Id, Description = state.Type == "Expenses" ? "Expense" : "Sale revenue and cost of goods sold",
                RevenueChange = state.Revenue, CostChange = state.Cost, ExpenseChange = state.Expense,
                IsReconstructed = true, IsEstimated = state.Estimated
            });
        }
        FinancialHistoryStarts.Add(new FinancialHistoryStart { ID = 1, RecordedAt = now });
        await base.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
