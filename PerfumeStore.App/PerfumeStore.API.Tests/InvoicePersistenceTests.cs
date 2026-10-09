using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Infrastructure.Persistence;
using PerfumeStore.Infrastructure.Repositories;
using Xunit;

namespace PerfumeStore.API.Tests;

// A real relational provider verifies constraints, transactions, and persisted
// batches without touching the owner's SQL Server database.
public sealed class InvoicePersistenceTests : IAsyncLifetime {
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private PerfumeStoreDbContext db = null!;
    private Product product = null!;
    private Supplier supplier = null!;

    public async Task InitializeAsync() {
        await connection.OpenAsync();
        db = new SqliteTestDbContext(new DbContextOptionsBuilder<PerfumeStoreDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        supplier = new Supplier { Name = "Test supplier" };
        supplier.ChangePhone("0599999999");
        product = new Product {
            Name = "Xerjoff Naxos", Capacity = 100, CostPrice = 750, SalePrice = 900,
            ProductCategory = new ProductCategory { Name = "Perfume" }
        };
        db.AddRange(supplier, product);
        await db.SaveChangesAsync();
        db.Inventory.Add(new Inventory { ProductID = product.ID, Quantity = 4 });
        db.InventoryBatches.Add(new InventoryBatch {
            ProductID = product.ID, OriginalQuantity = 4, RemainingQuantity = 4,
            UnitCost = 750, ReceivedAt = DateTime.MinValue, Source = "Opening stock", IsCostEstimated = true
        });
        await db.SaveChangesAsync();
    }

    private async Task<PurchaseInvoice> Purchase(int quantity = 2, decimal cost = 780) {
        var invoice = new PurchaseInvoice { SupplierId = supplier.Id, Date = new DateTime(2026, 10, 5) };
        await new PurchaseInvoicesRepository(db).AddAsync(invoice,
            new Dictionary<int, int> { [product.ID] = quantity },
            new Dictionary<int, decimal> { [product.ID] = cost });
        return invoice;
    }

    private async Task<SalesInvoice> Sell(int quantity, decimal price) {
        var invoice = new SalesInvoice { Date = new DateTime(2026, 10, 5) };
        await new SalesInvoicesRepository(db).AddAsync(invoice,
            new Dictionary<int, int> { [product.ID] = quantity },
            new Dictionary<int, decimal> { [product.ID] = price });
        return invoice;
    }

    [Fact]
    public async Task SupplierIncreasePreservesOldCostAndDefaultSalePrice() {
        await Purchase();
        await Sell(5, 900);
        db.ChangeTracker.Clear();
        var savedProduct = await db.Products.FindAsync(product.ID);
        Assert.Equal(750m, savedProduct!.CostPrice);
        Assert.Equal(900m, savedProduct.SalePrice);
        Assert.Equal(780m, (await db.PurchaseInvoiceItems.SingleAsync()).UnitCost);
        var sale = await db.SalesInvoiceItems.SingleAsync();
        Assert.Equal(3780m, sale.CostTotal);
        Assert.Equal(900m, sale.UnitPrice);
        Assert.Equal(1, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(1, await db.InventoryBatches.SumAsync(b => b.RemainingQuantity));
        Assert.Equal(2, await db.SalesCostAllocations.CountAsync());
    }

    [Fact]
    public async Task CustomerPricesSurviveCatalogChangesAndInvoiceEdit() {
        var first = await Sell(1, 30);
        await Sell(1, 25);
        product.SalePrice = 40;
        await db.SaveChangesAsync();
        await new SalesInvoicesRepository(db).UpdateProductsAsync(first.ID,
            new Dictionary<int, int> { [product.ID] = 1 });
        db.ChangeTracker.Clear();
        var prices = await db.SalesInvoiceItems.OrderBy(s => s.SalesInvoiceID).Select(s => s.UnitPrice).ToListAsync();
        Assert.Equal(new decimal?[] { 30m, 25m }, prices);
        Assert.All(await db.SalesInvoiceItems.ToListAsync(), s => Assert.Equal(750m, s.CostTotal));
        Assert.Equal(2, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(2, await db.InventoryBatches.SumAsync(b => b.RemainingQuantity));
    }

    [Fact]
    public async Task PurchaseCostCorrectionRecalculatesSoldStock() {
        var purchase = await Purchase();
        await Sell(6, 900);
        await new PurchaseInvoicesRepository(db).UpdateProductsAsync(purchase.ID,
            new Dictionary<int, int> { [product.ID] = 2 },
            new Dictionary<int, decimal> { [product.ID] = 800 });
        db.ChangeTracker.Clear();
        Assert.Equal(4600m, (await db.SalesInvoiceItems.SingleAsync()).CostTotal);
        Assert.Equal(0, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(900m, (await db.SalesInvoiceItems.SingleAsync()).UnitPrice);
    }

    [Fact]
    public async Task ManualAdditionRequiresCostAndKeepsSeparateBatch() {
        var repository = new InventoryRepository(db);
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.BusinessRuleException>(() =>
            repository.UpdateQuantityAsync(product.ID, 6, null));
        db.ChangeTracker.Clear();
        await repository.UpdateQuantityAsync(product.ID, 6, 780);
        await Sell(5, 900);
        db.ChangeTracker.Clear();
        Assert.Equal(3780m, (await db.SalesInvoiceItems.SingleAsync()).CostTotal);
        var added = await db.InventoryBatches.SingleAsync(b => b.Source == "Manual addition");
        Assert.Equal(780m, added.UnitCost);
        Assert.False(added.IsCostEstimated);
        Assert.Equal(1, added.RemainingQuantity);
    }

    [Fact]
    public async Task LegacyPurchaseCorrectionPreservesStockAndRecalculatesEstimatedSale() {
        var receipt = new PurchaseInvoice { SupplierId = supplier.Id, Date = new DateTime(2025, 1, 1) };
        var sale = new SalesInvoice { Date = new DateTime(2025, 1, 2) };
        db.AddRange(receipt, sale);
        await db.SaveChangesAsync();
        db.PurchaseInvoiceItems.Add(new PurchaseInvoiceItem {
            PurchaseInvoiceID = receipt.ID, ProductID = product.ID, Quantity = 6,
            UnitCost = 750, IsPriceEstimated = true
        });
        db.SalesInvoiceItems.Add(new SalesInvoiceItem {
            SalesInvoiceID = sale.ID, ProductID = product.ID, Quantity = 2,
            UnitPrice = 900, CostTotal = 1500, IsCostEstimated = true, IsPriceEstimated = true
        });
        await db.SaveChangesAsync();
        await new PurchaseInvoicesRepository(db).UpdateProductsAsync(receipt.ID,
            new Dictionary<int, int> { [product.ID] = 6 },
            new Dictionary<int, decimal> { [product.ID] = 780 });
        db.ChangeTracker.Clear();
        Assert.Equal(4, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(4, await db.InventoryBatches.SumAsync(b => b.RemainingQuantity));
        Assert.Equal(1560m, (await db.SalesInvoiceItems.SingleAsync()).CostTotal);
        Assert.Equal(900m, (await db.SalesInvoiceItems.SingleAsync()).UnitPrice);
        Assert.True((await db.SalesInvoiceItems.SingleAsync()).IsCostEstimated);
    }

    [Fact]
    public async Task InsufficientPurchaseCorrectionRollsBackReceiptAndCosts() {
        var purchase = await Purchase();
        await Sell(6, 900);
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.BusinessRuleException>(() =>
            new PurchaseInvoicesRepository(db).UpdateProductsAsync(purchase.ID,
                new Dictionary<int, int> { [product.ID] = 1 }));
        db.ChangeTracker.Clear();
        Assert.Equal(2, (await db.PurchaseInvoiceItems.SingleAsync()).Quantity);
        Assert.Equal(4560m, (await db.SalesInvoiceItems.SingleAsync()).CostTotal);
        Assert.Equal(0, (await db.Inventory.SingleAsync()).Quantity);
    }

    [Fact]
    public async Task FullySoldInvoiceCanBeCorrectedThroughHandler() {
        var sale = await Sell(4, 900);
        var account = new MoneyAccount { AccountName = "Test account", CurrentBalance = 0 };
        db.Add(account);
        await db.SaveChangesAsync();
        var handler = new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommandHandler(
            new SalesInvoicesRepository(db), new DebtsRepository(db), new MoneyAccountsRepository(db),
            new InventoryRepository(db), new ProductsRepository(db));
        await handler.Handle(new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommand {
            ID = sale.ID, Date = sale.Date, Notes = "Corrected price", AmountPaid = 2550, MoneyAccountId = account.ID,
            Products = new Dictionary<int, int> { [product.ID] = 3 },
            UnitPrices = new Dictionary<int, decimal> { [product.ID] = 850 }
        }, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(1, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(2250m, (await db.SalesInvoiceItems.SingleAsync()).CostTotal);
        Assert.Equal(850m, (await db.SalesInvoiceItems.SingleAsync()).UnitPrice);
    }

    [Theory]
    [InlineData(110, 70, 40, 0)]
    [InlineData(60, 20, 0, 10)]
    [InlineData(70, 30, 0, 0)]
    public async Task SaleCorrectionPreservesVoucherPaymentsAndRecordsCredit(decimal total, decimal originalDebt, decimal remaining, decimal credit) {
        var customer = new Customer { Name = "Test customer" };
        customer.ChangePhone("0599999998");
        var account = new MoneyAccount { AccountName = "Cash", CurrentBalance = 70 };
        db.AddRange(customer, account);
        await db.SaveChangesAsync();
        var sale = await Sell(1, 100);
        sale.CustomerId = customer.Id;
        sale.AmountPaid = 40;
        sale.MoneyAccountId = account.ID;
        sale.Debt = new Debt { Amount = 30, PersonId = customer.Id, Direction = DebtDirection.Receivable, SalesInvoiceId = sale.ID };
        var voucher = new ReceiptVoucher { Amount = 30, CustomerId = customer.Id, MoneyAccountID = account.ID };
        voucher.AppliedSalesInvoices.Add(new ReceiptVoucherSalesInvoice { SalesInvoiceId = sale.ID, AppliedAmount = 30 });
        db.Add(voucher);
        await db.SaveChangesAsync();
        var handler = new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommandHandler(
            new SalesInvoicesRepository(db), new DebtsRepository(db), new MoneyAccountsRepository(db), new InventoryRepository(db), new ProductsRepository(db));
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.BusinessRuleException>(() => handler.Handle(
            new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommand {
                ID = sale.ID, Date = sale.Date, CustomerId = customer.Id, AmountPaid = 41, MoneyAccountId = account.ID,
                HasDebt = true, DebtAmount = originalDebt,
                Products = new Dictionary<int, int> { [product.ID] = 1 }, UnitPrices = new Dictionary<int, decimal> { [product.ID] = total }
            }, CancellationToken.None));
        Assert.Equal(70, account.CurrentBalance);
        Assert.Equal(30, sale.Debt.Amount);
        await handler.Handle(new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommand {
            ID = sale.ID, Date = sale.Date, CustomerId = customer.Id, AmountPaid = 40, MoneyAccountId = account.ID,
            HasDebt = true, DebtAmount = originalDebt,
            Products = new Dictionary<int, int> { [product.ID] = 1 }, UnitPrices = new Dictionary<int, decimal> { [product.ID] = total }
        }, CancellationToken.None);
        db.ChangeTracker.Clear();
        var saved = (await new SalesInvoicesRepository(db).GetByIdAsync(sale.ID))!;
        Assert.Equal(remaining, saved.Debt!.Amount);
        Assert.Equal(remaining == 0, saved.Debt.IsDeleted);
        Assert.Equal(30, saved.ReceiptVoucherSalesInvoices.Sum(v => v.AppliedAmount));
        Assert.Equal(70, (await db.MoneyAccounts.SingleAsync()).CurrentBalance);
        var dto = new PerfumeStore.Application.SalesInvoices.Dtos.SalesInvoiceDetailsDto {
            AmountPaid = saved.AmountPaid, DebtAmount = saved.Debt.Amount, AppliedVoucherPayments = 30,
            Products = [new() { Quantity = 1, UnitPrice = total }]
        };
        Assert.Equal(originalDebt, dto.OriginalDebtAmount);
        Assert.Equal(credit, dto.CreditRefundDue);
    }

    [Theory]
    [InlineData(110, 70, 40, 0)]
    [InlineData(60, 20, 0, 10)]
    [InlineData(70, 30, 0, 0)]
    public async Task PurchaseCorrectionPreservesVoucherPaymentsAndRecordsCredit(decimal total, decimal originalDebt, decimal remaining, decimal credit) {
        var account = new MoneyAccount { AccountName = "Cash", CurrentBalance = 70 };
        db.Add(account);
        await db.SaveChangesAsync();
        var sale = await Purchase(1, 100);
        sale.SupplierId = supplier.Id;
        sale.AmountPaid = 40;
        sale.MoneyAccountId = account.ID;
        sale.Debt = new Debt { Amount = 30, PersonId = supplier.Id, Direction = DebtDirection.Payable, PurchaseInvoiceId = sale.ID };
        var voucher = new PaymentVoucher { Amount = 30, SupplierId = supplier.Id, MoneyAccountID = account.ID };
        voucher.AppliedPurchaseInvoices.Add(new PaymentVoucherPurchaseInvoice { PurchaseInvoiceId = sale.ID, AppliedAmount = 30 });
        db.Add(voucher);
        await db.SaveChangesAsync();
        var handler = new PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice.UpdatePurchaseInvoiceCommandHandler(
            new PurchaseInvoicesRepository(db), new DebtsRepository(db), new MoneyAccountsRepository(db), new ProductsRepository(db));
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.BusinessRuleException>(() => handler.Handle(
            new PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice.UpdatePurchaseInvoiceCommand {
                ID = sale.ID, Date = sale.Date, SupplierId = supplier.Id, AmountPaid = 41, MoneyAccountId = account.ID,
                HasDebt = true, DebtAmount = originalDebt,
                Products = new Dictionary<int, int> { [product.ID] = 1 }, UnitCosts = new Dictionary<int, decimal> { [product.ID] = total }
            }, CancellationToken.None));
        Assert.Equal(70, account.CurrentBalance);
        Assert.Equal(30, sale.Debt.Amount);
        await handler.Handle(new PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice.UpdatePurchaseInvoiceCommand {
            ID = sale.ID, Date = sale.Date, SupplierId = supplier.Id, AmountPaid = 40, MoneyAccountId = account.ID,
            HasDebt = true, DebtAmount = originalDebt,
            Products = new Dictionary<int, int> { [product.ID] = 1 }, UnitCosts = new Dictionary<int, decimal> { [product.ID] = total }
        }, CancellationToken.None);
        db.ChangeTracker.Clear();
        var saved = (await new PurchaseInvoicesRepository(db).GetByIdAsync(sale.ID))!;
        Assert.Equal(remaining, saved.Debt!.Amount);
        Assert.Equal(remaining == 0, saved.Debt.IsDeleted);
        Assert.Equal(30, saved.PaymentVoucherPurchaseInvoices.Sum(v => v.AppliedAmount));
        Assert.Equal(70, (await db.MoneyAccounts.SingleAsync()).CurrentBalance);
        var dto = new PerfumeStore.Application.PurchaseInvoices.Dtos.PurchaseInvoiceDetailsDto {
            AmountPaid = saved.AmountPaid, DebtAmount = saved.Debt.Amount, AppliedVoucherPayments = 30,
            Products = [new() { Quantity = 1, UnitCost = total }]
        };
        Assert.Equal(originalDebt, dto.OriginalDebtAmount);
        Assert.Equal(credit, dto.CreditRefundDue);
    }

    [Fact]
    public async Task ManualDecreaseRemovesOldestRemainingUnitsWithoutRepricingPastSale() {
        await Purchase();
        await Sell(3, 900);
        await new InventoryRepository(db).UpdateQuantityAsync(product.ID, 2, null);
        await Sell(1, 900);
        db.ChangeTracker.Clear();
        var sales = await db.SalesInvoiceItems.OrderBy(s => s.SalesInvoiceID).ToListAsync();
        Assert.Equal(2250m, sales[0].CostTotal);
        Assert.Equal(780m, sales[1].CostTotal);
        Assert.Equal(1, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(1, await db.InventoryBatches.SumAsync(b => b.RemainingQuantity));
    }

    [Fact]
    public async Task FailedSaleRollsBackInvoiceAndStock() {
        await Assert.ThrowsAsync<Exception>(() => Sell(10, 900));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.SalesInvoices.ToListAsync());
        Assert.Empty(await db.SalesCostAllocations.ToListAsync());
        Assert.Equal(4, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(4, await db.InventoryBatches.SumAsync(b => b.RemainingQuantity));
    }

    [Fact]
    public async Task DashboardUsesSoldStockCostsAndSelectedMonthInsteadOfPurchaseSpending() {
        await Purchase(2, 780);
        await Sell(2, 850);
        var account = new MoneyAccount { AccountName = "Test cash", CurrentBalance = 0 };
        var type = new ExpenseType { Name = "Test expense" };
        db.Expenses.Add(new Expense { Date = new DateTime(2026, 10, 6), Amount = 50,
            ExpenseType = type, MoneyAccount = account });
        await db.SaveChangesAsync();
        var dashboard = await new DashboardRepository(db).GetAsync(2026, 10);
        Assert.Equal(1700m, dashboard.SalesRevenue);
        Assert.Equal(1500m, dashboard.CostOfGoodsSold);
        Assert.Equal(200m, dashboard.GrossProfit);
        Assert.Equal(50m, dashboard.ExpenseTotal);
        Assert.Equal(150m, dashboard.NetProfit);
        Assert.Equal(200m, dashboard.Profit.Values[4]);
        Assert.Equal(-50m, dashboard.Profit.Values[5]);
        Assert.Equal(dashboard.NetProfit, dashboard.Profit.Values.Sum());
        var emptyMonth = await new DashboardRepository(db).GetAsync(2021, 1);
        Assert.Equal(0m, emptyMonth.NetProfit);
        Assert.Equal(31, emptyMonth.Profit.Values.Count);
        Assert.All(emptyMonth.Profit.Values, x => Assert.Equal(0m, x));
        var leapMonth = await new DashboardRepository(db).GetAsync(2024, 2);
        Assert.Equal(29, leapMonth.Profit.Values.Count);
    }

    [Fact]
    public async Task UnbalancedNewInvoicesAreRejectedWithoutChangingStockOrMoney() {
        var account = new MoneyAccount { AccountName = "Settlement test cash", CurrentBalance = 100 };
        db.MoneyAccounts.Add(account);
        await db.SaveChangesAsync();
        var salesHandler = new PerfumeStore.Application.SalesInvoices.Commands.CreateSalesInvoice.CreateSalesInvoiceCommandHandler(
            new SalesInvoicesRepository(db), new MoneyAccountsRepository(db), new InventoryRepository(db), null!, new ProductsRepository(db));
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.BusinessRuleException>(() => salesHandler.Handle(
            new PerfumeStore.Application.SalesInvoices.Commands.CreateSalesInvoice.CreateSalesInvoiceCommand {
                AmountPaid = 75, MoneyAccountId = account.ID,
                Products = new Dictionary<int, int> { [product.ID] = 1 },
                UnitPrices = new Dictionary<int, decimal> { [product.ID] = 80 }
            }, CancellationToken.None));
        var purchaseHandler = new PerfumeStore.Application.PurchaseInvoices.Commands.CreatePurchaseInvoice.CreatePurchaseInvoiceCommandHandler(
            new PurchaseInvoicesRepository(db), new MoneyAccountsRepository(db), null!, new ProductsRepository(db));
        await Assert.ThrowsAsync<PerfumeStore.Domain.Exceptions.BusinessRuleException>(() => purchaseHandler.Handle(
            new PerfumeStore.Application.PurchaseInvoices.Commands.CreatePurchaseInvoice.CreatePurchaseInvoiceCommand {
                SupplierId = supplier.Id, AmountPaid = 75, MoneyAccountId = account.ID,
                Products = new Dictionary<int, int> { [product.ID] = 1 },
                UnitCosts = new Dictionary<int, decimal> { [product.ID] = 80 }
            }, CancellationToken.None));
        Assert.Empty(await db.SalesInvoices.ToListAsync());
        Assert.Empty(await db.PurchaseInvoices.ToListAsync());
        Assert.Equal(4, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(100m, (await db.MoneyAccounts.SingleAsync()).CurrentBalance);
    }

    public async Task DisposeAsync() {
        await db.DisposeAsync();
        await connection.DisposeAsync();
    }
}

// Adapt SQL Server syntax only inside the test model. Production is unchanged.
internal sealed class SqliteTestDbContext(DbContextOptions<PerfumeStoreDbContext> options)
    : PerfumeStoreDbContext(options) {
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties())) {
            property.SetColumnType(null);
            if (property.GetDefaultValueSql() != null) property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
