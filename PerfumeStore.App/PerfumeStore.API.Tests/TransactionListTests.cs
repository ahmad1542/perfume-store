using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PerfumeStore.Application.PurchaseInvoices.Dtos;
using PerfumeStore.Application.SalesInvoices.Dtos;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Infrastructure.Persistence;
using PerfumeStore.Infrastructure.Repositories;
using Xunit;

namespace PerfumeStore.API.Tests;

public sealed class TransactionListTests : IAsyncLifetime {
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private PerfumeStoreDbContext db = null!;
    private readonly DateTime from = new(2026, 10, 20);
    private readonly DateTime to = new(2026, 10, 31);

    public async Task InitializeAsync() {
        await connection.OpenAsync();
        db = new SqliteTestDbContext(new DbContextOptionsBuilder<PerfumeStoreDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var safe = new MoneyAccount { AccountName = "Safe", CurrentBalance = 1000 };
        var bank = new MoneyAccount { AccountName = "Bank" };
        var customer = new Customer { Name = "List customer" }; customer.ChangePhone("0599999991");
        var supplier = new Supplier { Name = "List supplier" }; supplier.ChangePhone("0599999992");
        var product = new Product { Name = "Test", Capacity = 100, SalePrice = 90, CostPrice = 50,
            ProductCategory = new ProductCategory { Name = "Perfume" } };
        var type = new ExpenseType { Name = "Shop" };
        db.AddRange(safe, bank, customer, supplier, product, type); await db.SaveChangesAsync();
        // Direct fixtures intentionally focus on query boundaries, not invoice creation rules.
        foreach (var date in new[] { new DateTime(2026, 9, 30, 23, 59, 59), from, to.AddHours(23).AddMinutes(59).AddSeconds(59), new DateTime(2026, 11, 1) }) {
            db.AddRange(new Expense { Date = date, Amount = 10, ExpenseType = type, MoneyAccount = safe },
                new SalesInvoiceItem { Quantity = 2, UnitPrice = 9, CostTotal = 10, Product = product,
                    SalesInvoice = new SalesInvoice { Date = date, AmountPaid = 4, Customer = customer, MoneyAccount = safe } },
                new PurchaseInvoiceItem { Quantity = 3, UnitCost = 5, Product = product,
                    PurchaseInvoice = new PurchaseInvoice { Date = date, AmountPaid = 4, Supplier = supplier, MoneyAccount = safe } },
                new Debt { Date = date, Amount = 20, PersonId = customer.Id, Direction = DebtDirection.Receivable },
                new ReceiptVoucher { Date = date, Amount = 8, Customer = customer, MoneyAccount = safe },
                new PaymentVoucher { Date = date, Amount = 7, Supplier = supplier, MoneyAccount = safe },
                new MoneyTransaction { Date = date, TransferAmount = 6, FromMoneyAccount = safe, ToMoneyAccount = bank });
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    }

    [Theory]
    [InlineData("expenses")]
    [InlineData("sales")]
    [InlineData("purchases")]
    [InlineData("debts")]
    [InlineData("receipts")]
    [InlineData("payments")]
    [InlineData("transfers")]
    public async Task DateRangeIncludesEndDayAndExcludesOtherMonths(string kind) {
        IEnumerable<DateTime> dates = kind switch {
            "expenses" => (await new ExpensesRepository(db).GetAllAsync(null, from, to)).Select(x => x.Date),
            "sales" => (await new SalesInvoicesRepository(db).GetAllAsync(null, from, to)).Select(x => x.Date),
            "purchases" => (await new PurchaseInvoicesRepository(db).GetAllAsync(null, from, to)).Select(x => x.Date),
            "debts" => (await new DebtsRepository(db).GetAllAsync(false, null, from, to)).Select(x => x.Date),
            "receipts" => (await new ReceiptVouchersRepository(db).GetAllAsync(null, from, to)).Select(x => x.Date),
            "payments" => (await new PaymentVouchersRepository(db).GetAllAsync(null, from, to)).Select(x => x.Date),
            _ => (await new MoneyTransactionsRepository(db).GetAllAsync(null, from, to)).Select(x => x.Date)
        };
        Assert.Equal(2, dates.Count());
        Assert.Contains(from, dates);
        Assert.Contains(dates, x => x.Date == to && x.Hour == 23);
    }

    [Fact]
    public async Task DateRangesCombineWithSearchAndSettledFiltersAndCanBeCleared() {
        var debt = await db.Debts.SingleAsync(x => x.Date == from); debt.IsDeleted = true; await db.SaveChangesAsync();
        Assert.Single(await new DebtsRepository(db).GetAllAsync(false, null, from, to));
        Assert.Equal(2, (await new DebtsRepository(db).GetAllAsync(true, null, from, to)).Count());
        Assert.Empty(await new ReceiptVouchersRepository(db).GetAllAsync("does not exist", from, to));
        Assert.Equal(2, (await new ReceiptVouchersRepository(db).GetAllAsync("List customer", from, to)).Count());
        Assert.Equal(4, (await new ReceiptVouchersRepository(db).GetAllAsync()).Count());
        Assert.Equal(4, (await new MoneyTransactionsRepository(db).GetAllAsync()).Count());
    }

    [Fact]
    public async Task InvoiceTotalsUseSavedLinePricesAndDoNotShrinkWhenDebtIsCollected() {
        var sale = await db.SalesInvoices.SingleAsync(x => x.Date == from);
        var purchase = await db.PurchaseInvoices.SingleAsync(x => x.Date == from);
        sale.Debt = new Debt { Date = from, Amount = 2, Direction = DebtDirection.Receivable };
        purchase.Debt = new Debt { Date = from, Amount = 3, Direction = DebtDirection.Payable };
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var mapper = new MapperConfiguration(c => { c.AddProfile<SalesInvoicesProfile>(); c.AddProfile<PurchaseInvoicesProfile>(); }, NullLoggerFactory.Instance).CreateMapper();
        var sales = mapper.Map<List<SalesInvoiceDto>>(await new SalesInvoicesRepository(db).GetAllAsync(null, from, from));
        Assert.Equal(18, Assert.Single(sales).InvoiceTotal);
        Assert.Equal(4, sales[0].AmountPaid); Assert.Equal(2, sales[0].DebtAmount);
        var purchases = mapper.Map<List<PurchaseInvoiceDto>>(await new PurchaseInvoicesRepository(db).GetAllAsync(null, from, from));
        Assert.Equal(15, Assert.Single(purchases).InvoiceTotal);
        Assert.Equal(4, purchases[0].AmountPaid); Assert.Equal(3, purchases[0].DebtAmount);
    }

    [Fact]
    public async Task InvoiceFiltersMatchOneLineAndKeepWholeInvoiceAndMatchingTotalsSeparate() {
        var product = await db.Products.SingleAsync();
        product.Brand = new Brand { Name = "Brand A" };
        var other = new Product { Name = "Other", Capacity = 50, CostPrice = 7, SalePrice = 20,
            ProductCategory = new ProductCategory { Name = "Other category" }, Brand = new Brand { Name = "Brand B" } };
        var customer = await db.Customers.SingleAsync(); var supplier = await db.Suppliers.SingleAsync();
        var sale = new SalesInvoice { Date = from, Customer = customer };
        var purchase = new PurchaseInvoice { Date = from, Supplier = supplier };
        db.AddRange(new SalesInvoiceItem { SalesInvoice = sale, Product = product, Quantity = 1, UnitPrice = 9, CostTotal = 5 },
            new SalesInvoiceItem { SalesInvoice = sale, Product = other, Quantity = 2, UnitPrice = 20, CostTotal = 14 },
            new PurchaseInvoiceItem { PurchaseInvoice = purchase, Product = product, Quantity = 1, UnitCost = 5 },
            new PurchaseInvoiceItem { PurchaseInvoice = purchase, Product = other, Quantity = 2, UnitCost = 7 });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var filters = new PerfumeStore.Domain.Repositories.RecordFilters { CategoryIds = [other.ProductCategoryID], PartyId = customer.Id };
        var mapper = new MapperConfiguration(c => { c.AddProfile<SalesInvoicesProfile>(); c.AddProfile<PurchaseInvoicesProfile>(); }, NullLoggerFactory.Instance).CreateMapper();
        var handler = new PerfumeStore.Application.SalesInvoices.Queries.GetAllSalesinvoices.GetAllSalesInvoicesQueryHandler(new SalesInvoicesRepository(db),mapper);
        var result = Assert.Single(await handler.Handle(new PerfumeStore.Application.SalesInvoices.Queries.GetAllSalesinvoices.GetAllSalesinvoicesQuery { Filters=filters },default));
        Assert.Equal(49,result.InvoiceTotal); Assert.Equal(40,result.MatchingProductsTotal);
        filters.BrandId = product.BrandID;
        Assert.Empty(await new SalesInvoicesRepository(db).GetAllAsync(filters:filters));
        filters.BrandId=other.BrandID; filters.ProductId=other.ID;
        Assert.Single(await new SalesInvoicesRepository(db).GetAllAsync(filters:filters));
        filters.PartyId=supplier.Id;
        var ph = new PerfumeStore.Application.PurchaseInvoices.Queries.GetAllPurchaseinvoices.GetAllPurchaseInvoicesQueryHandler(new PurchaseInvoicesRepository(db),mapper);
        var pr = Assert.Single(await ph.Handle(new PerfumeStore.Application.PurchaseInvoices.Queries.GetAllPurchaseinvoices.GetAllPurchaseinvoicesQuery { Filters=filters },default));
        Assert.Equal(19,pr.InvoiceTotal); Assert.Equal(14,pr.MatchingProductsTotal);
        filters.BrandId=product.BrandID;
        Assert.Empty(await new PurchaseInvoicesRepository(db).GetAllAsync(filters:filters));
    }

    [Fact]
    public async Task FinancialFiltersApplyAccountsPartiesDirectionsStatusAndLinkedInvoices() {
        var safe=await db.MoneyAccounts.SingleAsync(x=>x.AccountName=="Safe");var bank=await db.MoneyAccounts.SingleAsync(x=>x.AccountName=="Bank");
        var customer=await db.Customers.SingleAsync();var supplier=await db.Suppliers.SingleAsync();
        var debt=await db.Debts.SingleAsync(x=>x.Date==from);debt.IsDeleted=true;
        var receipt=await db.ReceiptVouchers.SingleAsync(x=>x.Date==from);
        var payment=await db.PaymentVouchers.SingleAsync(x=>x.Date==from);
        var sale=await db.SalesInvoices.SingleAsync(x=>x.Date==from);var purchase=await db.PurchaseInvoices.SingleAsync(x=>x.Date==from);
        receipt.AppliedSalesInvoices.Add(new ReceiptVoucherSalesInvoice { SalesInvoiceId=sale.ID,AppliedAmount=8 });
        payment.AppliedPurchaseInvoices.Add(new PaymentVoucherPurchaseInvoice { PurchaseInvoiceId=purchase.ID,AppliedAmount=7 });
        await db.SaveChangesAsync();
        var f=new PerfumeStore.Domain.Repositories.RecordFilters { AccountId=bank.ID };
        Assert.Empty(await new ExpensesRepository(db).GetAllAsync(filters:f));
        Assert.Empty(await new PaymentVouchersRepository(db).GetAllAsync(filters:f));
        f.AccountId=safe.ID;f.PartyId=customer.Id;f.PartyType="customer";f.InvoiceId=sale.ID;
        Assert.Single(await new ReceiptVouchersRepository(db).GetAllAsync(filters:f));
        f.PartyType="person";Assert.Empty(await new ReceiptVouchersRepository(db).GetAllAsync(filters:f));
        f=new() { PartyId=supplier.Id,InvoiceId=purchase.ID,AccountId=safe.ID };
        Assert.Single(await new PaymentVouchersRepository(db).GetAllAsync(filters:f));
        f=new() { Direction=2 };Assert.Empty(await new DebtsRepository(db).GetAllAsync(false,filters:f));
        f=new() { PartyType="customer",Status="settled",PartyId=customer.Id };
        Assert.Single(await new DebtsRepository(db).GetAllAsync(false,filters:f));
        f.Status="all";Assert.Equal(4,(await new DebtsRepository(db).GetAllAsync(false,filters:f)).Count());
        f.PartyType="supplier";Assert.Empty(await new DebtsRepository(db).GetAllAsync(true,filters:f));
        f=new() { FromAccountId=safe.ID,ToAccountId=bank.ID };
        Assert.Equal(4,(await new MoneyTransactionsRepository(db).GetAllAsync(filters:f)).Count());
        f.FromAccountId=bank.ID;Assert.Empty(await new MoneyTransactionsRepository(db).GetAllAsync(filters:f));
    }

    [Fact]
    public async Task InventoryFiltersRespectBrandCategoryAndStockThresholds() {
        var p=await db.Products.SingleAsync();p.MinStock=5;p.Brand=new Brand { Name="A" };
        var other=new Product { Name="Empty",Capacity=1,ProductCategory=new ProductCategory { Name="Empty" },Brand=new Brand { Name="B" } };
        db.AddRange(new Inventory { Product=p,Quantity=4 },new Inventory { Product=other,Quantity=0 });await db.SaveChangesAsync();
        var f=new PerfumeStore.Domain.Repositories.RecordFilters { StockStatus="low-stock" };
        Assert.Equal(p.ID,Assert.Single(await new InventoryRepository(db).GetAllAsync(filters:f)).ProductID);
        f.StockStatus="out-of-stock";f.BrandId=other.BrandID;f.CategoryIds=[other.ProductCategoryID];
        Assert.Equal(other.ID,Assert.Single(await new InventoryRepository(db).GetAllAsync(filters:f)).ProductID);
        f.StockStatus="in-stock";Assert.Empty(await new InventoryRepository(db).GetAllAsync(filters:f));
    }

    [Fact]
    public async Task BrandLimitSupportsFiveTenAndAllWithoutChangingProfit() {
        var category=await db.ProductCategories.SingleAsync();
        for(int i=0;i<6;i++) db.Add(new SalesInvoiceItem { Quantity=1,UnitPrice=10+i,CostTotal=1,
            SalesInvoice=new SalesInvoice { Date=from },Product=new Product { Name="Brand product "+i,Capacity=10,ProductCategory=category,Brand=new Brand { Name="Brand "+i } } });
        await db.SaveChangesAsync();var repository=new DashboardRepository(db);
        var five=await repository.GetAsync(2026,10,5);var ten=await repository.GetAsync(2026,10,10);var all=await repository.GetAsync(2026,10,0);
        Assert.Equal(5,five.Brands.Count);Assert.Equal(6,ten.Brands.Count);Assert.Equal(6,all.Brands.Count);
        Assert.Equal(five.NetProfit,all.NetProfit);Assert.Equal("Brand 5",all.Brands[0].Name);
    }

    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
}
