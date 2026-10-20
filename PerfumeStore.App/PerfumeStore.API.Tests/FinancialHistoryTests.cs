using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PerfumeStore.Application.ReceiptVouchers.Commands.CreateReceiptVoucher;
using PerfumeStore.Application.ReceiptVouchers.Dtos;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Infrastructure.Persistence;
using PerfumeStore.Infrastructure.Repositories;
using Xunit;

namespace PerfumeStore.API.Tests;

public sealed class FinancialHistoryTests : IAsyncLifetime {
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private PerfumeStoreDbContext db = null!;
    private static DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem")).Date;

    public async Task InitializeAsync() {
        await connection.OpenAsync();
        db = new SqliteTestDbContext(new DbContextOptionsBuilder<PerfumeStoreDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
    }

    private async Task<(MoneyAccount Account, SalesInvoice Sale, SalesInvoiceItem Item, Customer Customer)> SeedSale(DateTime date) {
        var account = new MoneyAccount { AccountName = "Safe", CurrentBalance = 500 };
        var customer = new Customer { Name = "Test customer" };
        customer.ChangePhone("0599999999");
        var product = new Product { Name = "Test perfume", Capacity = 100, CostPrice = 25, SalePrice = 75,
            ProductCategory = new ProductCategory { Name = "Perfume" } };
        var sale = new SalesInvoice { Date = date, Customer = customer,
            Debt = new Debt { Date = date, Amount = 75, Direction = DebtDirection.Receivable, PersonId = customer.Id } };
        var item = new SalesInvoiceItem { Product = product, SalesInvoice = sale, Quantity = 1, UnitPrice = 75, CostTotal = 25 };
        db.AddRange(account, item);
        await db.SaveChangesAsync();
        return (account, sale, item, customer);
    }

    private Task<PerfumeStore.Application.FinancialHistory.FinancialHistoryDto> Report(string kind, DateTime date, int? account = null) =>
        new FinancialHistoryRepository(db).GetAsync(kind, date.Year, date.Month, account);

    [Fact]
    public async Task CollectingPriorMonthDebtIncreasesCashNotProfitAndExpenseOffsetsCash() {
        await db.InitializeFinancialHistoryAsync();
        var previousMonth = new DateTime(Today.Year, Today.Month, 1).AddMonths(-1);
        var (account, sale, _, customer) = await SeedSale(previousMonth);
        db.HistoryContext = new FinancialHistoryContext { Resource = "ReceiptVouchers", Action = "Created" };
        var mapper = new MapperConfiguration(c => c.AddProfile<ReceiptVouchersProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var handler = new CreateReceiptVoucherCommandHandler(new ReceiptVouchersRepository(db), new MoneyAccountsRepository(db),
            new CustomersRepository(db), new PersonsRepository(db), new DebtsRepository(db), mapper);
        var id = await handler.Handle(new CreateReceiptVoucherCommand {
            Date = Today, CustomerId = customer.Id, MoneyAccountID = account.ID, Amount = 20,
            SalesApplications = [new CreateReceiptVoucherSalesApplicationDto { SalesInvoiceId = sale.ID, AppliedAmount = 20 }]
        }, CancellationToken.None);
        Assert.Equal(520, account.CurrentBalance);
        Assert.Equal(55, sale.Debt!.Amount);
        var cash = await Report("cash", Today, account.ID);
        Assert.Equal(0, cash.BalanceDifference);
        Assert.Contains(cash.Rows, x => x.SourceType == "ReceiptVouchers" && x.SourceId == id.ToString() && x.CashChange == 20);
        Assert.Equal(50, (await Report("profit", previousMonth)).NetProfit);
        var profit = await Report("profit", Today);
        Assert.Equal(0, profit.NetProfit);
        Assert.Contains(profit.Rows, x => x.SourceType == "ReceiptVouchers" && x.CashChange == 20 && x.ProfitChange == 0);
        db.HistoryContext = new FinancialHistoryContext { Resource = "Expenses", Action = "Created" };
        db.Expenses.Add(new Expense { Date = Today, Amount = 20, MoneyAccount = account, ExpenseType = new ExpenseType { Name = "Personal" } });
        account.CurrentBalance -= 20;
        await db.SaveChangesAsync();
        cash = await Report("cash", Today, account.ID);
        Assert.Equal(500, cash.ClosingBalance);
        Assert.Equal(0, cash.BalanceDifference);
        Assert.Equal(-20, (await Report("profit", Today)).NetProfit);
    }

    [Fact]
    public async Task TransfersAndDirectBalanceCorrectionsRemainTraceable() {
        await db.InitializeFinancialHistoryAsync();
        var safe = new MoneyAccount { AccountName = "Safe", CurrentBalance = 500 };
        var bank = new MoneyAccount { AccountName = "Bank" };
        db.AddRange(safe, bank); await db.SaveChangesAsync();
        db.HistoryContext = new FinancialHistoryContext { Resource = "MoneyTransactions", Action = "Created" };
        db.MoneyTransactions.Add(new MoneyTransaction { Date = Today, FromMoneyAccount = safe, ToMoneyAccount = bank, TransferAmount = 100 });
        safe.CurrentBalance -= 100; bank.CurrentBalance += 100;
        await db.SaveChangesAsync();
        Assert.Equal(400, (await Report("cash", Today, safe.ID)).ClosingBalance);
        Assert.Equal(100, (await Report("cash", Today, bank.ID)).ClosingBalance);
        var combined = await Report("cash", Today);
        Assert.Equal(500, combined.ClosingBalance);
        Assert.Equal(0, combined.BalanceDifference);
        db.HistoryContext = new FinancialHistoryContext { Resource = "MoneyAccounts", SourceId = safe.ID.ToString(), Action = "Corrected" };
        safe.CurrentBalance = 415; await db.SaveChangesAsync();
        Assert.Contains((await Report("cash", Today, safe.ID)).Rows, x => x.CashChange == 15 && x.Description.Contains("Corrected"));
        Assert.Equal(0, (await Report("profit", Today)).NetProfit);
    }

    [Fact]
    public async Task ProfitCorrectionsKeepOriginalEntriesAndMovingDateReversesOriginalMonth() {
        await db.InitializeFinancialHistoryAsync();
        var previousMonth = new DateTime(Today.Year, Today.Month, 1).AddMonths(-1);
        var (_, sale, item, _) = await SeedSale(previousMonth);
        db.HistoryContext = new FinancialHistoryContext { Resource = "PurchaseInvoices", SourceId = "12", Action = "Corrected" };
        item.CostTotal = 30; await db.SaveChangesAsync();
        var previous = await Report("profit", previousMonth);
        Assert.Equal(45, previous.NetProfit);
        Assert.Contains(previous.Rows, x => x.CostChange == 5 && x.Description.Contains("PurchaseInvoices #12"));
        Assert.Contains(previous.Rows, x => x.RevenueChange == 75 && x.CostChange == 25);
        db.HistoryContext = new FinancialHistoryContext { Resource = "SalesInvoices", SourceId = sale.ID.ToString(), Action = "Corrected" };
        sale.Date = Today; await db.SaveChangesAsync();
        Assert.Equal(0, (await Report("profit", previousMonth)).NetProfit);
        Assert.Equal(45, (await Report("profit", Today)).NetProfit);
        var dashboard = await new DashboardRepository(db).GetAsync(Today.Year, Today.Month);
        Assert.Equal(dashboard.NetProfit, (await Report("profit", Today)).NetProfit);
    }

    [Fact]
    public async Task RollingBackBusinessTransactionAlsoRollsBackHistory() {
        await db.InitializeFinancialHistoryAsync();
        var account = new MoneyAccount { AccountName = "Safe", CurrentBalance = 500 };
        db.Add(account); await db.SaveChangesAsync();
        var count = await db.FinancialHistoryEntries.CountAsync();
        await using (var tx = await db.Database.BeginTransactionAsync()) {
            account.CurrentBalance += 20; await db.SaveChangesAsync();
            Assert.Equal(count + 1, await db.FinancialHistoryEntries.CountAsync());
            await tx.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        Assert.Equal(count, await db.FinancialHistoryEntries.CountAsync());
        Assert.Equal(500, (await db.MoneyAccounts.SingleAsync()).CurrentBalance);
    }

    [Fact]
    public async Task ExistingDataIsReconstructedOnceWithoutChangingBalancesOrProfit() {
        // Synchronous base SaveChanges deliberately simulates records predating
        // the async financial tracker, as the real database does at migration.
        var account = new MoneyAccount { AccountName = "Safe", CurrentBalance = 785 };
        db.Add(account); db.SaveChanges();
        db.Expenses.Add(new Expense { Amount = 20, Date = Today, MoneyAccount = account, ExpenseType = new ExpenseType { Name = "Personal" } });
        db.SaveChanges();
        await db.InitializeFinancialHistoryAsync();
        var count = await db.FinancialHistoryEntries.CountAsync();
        await db.InitializeFinancialHistoryAsync();
        Assert.Equal(count, await db.FinancialHistoryEntries.CountAsync());
        Assert.Equal(785, account.CurrentBalance);
        var cash = await Report("cash", Today);
        Assert.Equal(805, cash.OpeningBalance);
        Assert.Equal(785, cash.ClosingBalance);
        Assert.Equal(0, cash.BalanceDifference);
        Assert.All(cash.Rows, x => Assert.True(x.IsReconstructed));
        Assert.Equal(-20, (await Report("profit", Today)).NetProfit);
    }

    [Fact]
    public async Task CompletedFifoCalculationClearsTemporaryEstimateEvenWhenCostIsUnchanged() {
        await db.InitializeFinancialHistoryAsync();
        db.HistoryContext = new FinancialHistoryContext { Resource = "SalesInvoices", Action = "Created" };
        var (_, sale, item, _) = await SeedSale(Today);
        item.CostTotal = null; await db.SaveChangesAsync();
        item.CostTotal = 25; await db.SaveChangesAsync();
        var row = Assert.Single((await Report("profit", Today)).Rows, x => x.SourceId == sale.ID.ToString());
        Assert.False(row.IsEstimated);
        Assert.Equal(50, row.ProfitChange);
    }

    [Fact]
    public async Task DeletingExpenseRestoresCashAndReversesOriginalMonthProfitOnlyOnce() {
        await db.InitializeFinancialHistoryAsync();
        var previousMonth = new DateTime(Today.Year, Today.Month, 1).AddMonths(-1);
        var account = new MoneyAccount { AccountName = "Safe", CurrentBalance = 500 };
        db.Add(account); await db.SaveChangesAsync();
        var expense = new Expense { Date = previousMonth, Amount = 20, MoneyAccount = account,
            ExpenseType = new ExpenseType { Name = "Personal" } };
        db.HistoryContext = new FinancialHistoryContext { Resource = "Expenses", Action = "Created" };
        db.Add(expense); account.CurrentBalance -= expense.Amount; await db.SaveChangesAsync();
        Assert.Equal(-20, (await Report("profit", previousMonth)).NetProfit);
        db.HistoryContext = new FinancialHistoryContext { Resource = "Expenses", SourceId = expense.ID.ToString(), Action = "Deleted" };
        var handler = new PerfumeStore.Application.Expenses.Commands.DeleteExpense.DeleteExpenseCommandHandler(
            new ExpensesRepository(db), new MoneyAccountsRepository(db));
        var command = new PerfumeStore.Application.Expenses.Commands.DeleteExpense.DeleteExpenseCommand(expense.ID);
        await handler.Handle(command, CancellationToken.None);
        Assert.Empty(await db.Expenses.ToListAsync());
        Assert.Equal(500, account.CurrentBalance);
        var cash = await Report("cash", Today);
        Assert.Equal(0, cash.BalanceDifference);
        Assert.Contains(cash.Rows, x => x.SourceType == "Expenses" && x.CashChange == 20 && x.Description.Contains("Deleted"));
        var profit = await Report("profit", previousMonth);
        Assert.Equal(0, profit.NetProfit);
        Assert.Equal(2, profit.Rows.Count);
        Assert.All(profit.Rows, x => Assert.True(x.IsSourceDeleted));
        Assert.Contains(profit.Rows, x => x.ExpenseChange == -20 && x.ProfitChange == 20);
        Assert.Equal(0, (await Report("profit", Today)).NetProfit);
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(500, account.CurrentBalance);
    }

    [Fact]
    public async Task DeletingEditedExpenseRestoresItsCurrentAmountToItsCurrentAccount() {
        await db.InitializeFinancialHistoryAsync();
        var safe = new MoneyAccount { AccountName = "Safe", CurrentBalance = 500 };
        var bank = new MoneyAccount { AccountName = "Bank", CurrentBalance = 100 };
        db.AddRange(safe, bank); await db.SaveChangesAsync();
        var expense = new Expense { Date = Today, Amount = 20, MoneyAccount = safe,
            ExpenseType = new ExpenseType { Name = "Personal" } };
        db.Add(expense); safe.CurrentBalance -= 20; await db.SaveChangesAsync();
        safe.CurrentBalance += 20; bank.CurrentBalance -= 35;
        expense.MoneyAccount = bank; expense.Amount = 35; await db.SaveChangesAsync();
        await new PerfumeStore.Application.Expenses.Commands.DeleteExpense.DeleteExpenseCommandHandler(
            new ExpensesRepository(db), new MoneyAccountsRepository(db)).Handle(
                new PerfumeStore.Application.Expenses.Commands.DeleteExpense.DeleteExpenseCommand(expense.ID), CancellationToken.None);
        Assert.Equal(500, safe.CurrentBalance);
        Assert.Equal(100, bank.CurrentBalance);
        Assert.Equal(0, (await Report("profit", Today)).NetProfit);
        Assert.Equal(0, (await Report("cash", Today)).BalanceDifference);
    }

    [Fact]
    public async Task UntrackedBalanceChangesAreReportedAsDifferencesNotSilentlyRepaired() {
        await db.InitializeFinancialHistoryAsync();
        var account = new MoneyAccount { AccountName = "Safe", CurrentBalance = 500 };
        db.Add(account); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE MoneyAccounts SET CurrentBalance = 510");
        var report = await Report("cash", Today);
        Assert.Equal(10, report.BalanceDifference);
        Assert.Equal(500, report.ClosingBalance);
    }

    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
}
