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
        var handler = new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommandHandler(
            new SalesInvoicesRepository(db), new DebtsRepository(db), new MoneyAccountsRepository(db),
            new InventoryRepository(db));
        await handler.Handle(new PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice.UpdateSalesInvoiceCommand {
            ID = sale.ID, Date = sale.Date, Notes = "Corrected price",
            Products = new Dictionary<int, int> { [product.ID] = 3 },
            UnitPrices = new Dictionary<int, decimal> { [product.ID] = 850 }
        }, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(1, (await db.Inventory.SingleAsync()).Quantity);
        Assert.Equal(2250m, (await db.SalesInvoiceItems.SingleAsync()).CostTotal);
        Assert.Equal(850m, (await db.SalesInvoiceItems.SingleAsync()).UnitPrice);
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
