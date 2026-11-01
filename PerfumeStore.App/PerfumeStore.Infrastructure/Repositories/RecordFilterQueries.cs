using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories;

internal static class RecordFilterQueries {
    public static IQueryable<SalesInvoice> Filter(this IQueryable<SalesInvoice> query, RecordFilters? f) {
        if (f == null) return query;
        if (f.PartyId.HasValue) query = query.Where(x => x.CustomerId == f.PartyId);
        if (f.HasLineFilters) query = query.Where(x => x.SalesInvoiceItems.Any(i =>
            (f.CategoryIds.Count == 0 || f.CategoryIds.Contains(i.Product.ProductCategoryID)) &&
            (!f.BrandId.HasValue || i.Product.BrandID == f.BrandId) && (!f.ProductId.HasValue || i.ProductID == f.ProductId)));
        return query;
    }
    public static IQueryable<PurchaseInvoice> Filter(this IQueryable<PurchaseInvoice> query, RecordFilters? f) {
        if (f == null) return query;
        if (f.PartyId.HasValue) query = query.Where(x => x.SupplierId == f.PartyId);
        if (f.HasLineFilters) query = query.Where(x => x.PurchaseInvoiceItems.Any(i =>
            (f.CategoryIds.Count == 0 || f.CategoryIds.Contains(i.Product.ProductCategoryID)) &&
            (!f.BrandId.HasValue || i.Product.BrandID == f.BrandId) && (!f.ProductId.HasValue || i.ProductID == f.ProductId)));
        return query;
    }
    public static IQueryable<Expense> Filter(this IQueryable<Expense> query, RecordFilters? f) =>
        f?.AccountId is int account ? query.Where(x => x.MoneyAccountID == account) : query;
    public static IQueryable<Debt> Filter(this IQueryable<Debt> query, RecordFilters? f, PerfumeStoreDbContext db) {
        if (f == null) return query;
        if (f.PartyId.HasValue) query = query.Where(x => x.PersonId == f.PartyId);
        if (f.Direction.HasValue) query = query.Where(x => (int)x.Direction == f.Direction);
        if (f.Status == "settled") query = query.Where(x => x.IsDeleted);
        if (f.Status == "open") query = query.Where(x => !x.IsDeleted);
        if (f.PartyType == "customer") query = query.Where(x => db.Customers.Any(p => p.Id == x.PersonId));
        if (f.PartyType == "supplier") query = query.Where(x => db.Suppliers.Any(p => p.Id == x.PersonId));
        if (f.PartyType == "person") query = query.Where(x => x.PersonId != null && !db.Customers.Any(p => p.Id == x.PersonId) && !db.Suppliers.Any(p => p.Id == x.PersonId));
        return query;
    }
    public static IQueryable<ReceiptVoucher> Filter(this IQueryable<ReceiptVoucher> query, RecordFilters? f) {
        if (f == null) return query;
        if (f.PartyType == "customer") query = query.Where(x => x.CustomerId != null);
        if (f.PartyType == "person") query = query.Where(x => x.CustomerId == null && x.PersonId != null);
        if (f.PartyId.HasValue) query = query.Where(x => x.CustomerId == f.PartyId || x.PersonId == f.PartyId);
        if (f.AccountId.HasValue) query = query.Where(x => x.MoneyAccountID == f.AccountId);
        if (f.InvoiceId.HasValue) query = query.Where(x => x.AppliedSalesInvoices.Any(a => a.SalesInvoiceId == f.InvoiceId));
        return query;
    }
    public static IQueryable<PaymentVoucher> Filter(this IQueryable<PaymentVoucher> query, RecordFilters? f) {
        if (f == null) return query;
        if (f.PartyId.HasValue) query = query.Where(x => x.SupplierId == f.PartyId);
        if (f.AccountId.HasValue) query = query.Where(x => x.MoneyAccountID == f.AccountId);
        if (f.InvoiceId.HasValue) query = query.Where(x => x.AppliedPurchaseInvoices.Any(a => a.PurchaseInvoiceId == f.InvoiceId));
        return query;
    }
    public static IQueryable<MoneyTransaction> Filter(this IQueryable<MoneyTransaction> query, RecordFilters? f) {
        if (f?.FromAccountId is int from) query = query.Where(x => x.FromMoneyAccountID == from);
        if (f?.ToAccountId is int to) query = query.Where(x => x.ToMoneyAccountID == to);
        return query;
    }
    public static IQueryable<Inventory> Filter(this IQueryable<Inventory> query, RecordFilters? f) {
        if (f == null) return query;
        if (f.CategoryIds.Count > 0) query = query.Where(x => f.CategoryIds.Contains(x.Product.ProductCategoryID));
        if (f.BrandId.HasValue) query = query.Where(x => x.Product.BrandID == f.BrandId);
        if (f.StockStatus == "in-stock") query = query.Where(x => x.Quantity > 0);
        if (f.StockStatus == "out-of-stock") query = query.Where(x => x.Quantity <= 0);
        if (f.StockStatus == "low-stock") query = query.Where(x => x.Product.MinStock.HasValue && x.Quantity <= x.Product.MinStock);
        return query;
    }
}
