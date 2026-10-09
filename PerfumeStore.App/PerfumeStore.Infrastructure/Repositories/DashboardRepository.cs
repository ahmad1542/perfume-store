using Microsoft.EntityFrameworkCore;
using PerfumeStore.Application.Dashboard.Dtos;
using PerfumeStore.Application.Dashboard.Interfaces;
using PerfumeStore.Infrastructure.Persistence;

namespace PerfumeStore.Infrastructure.Repositories {
    public class DashboardRepository(PerfumeStoreDbContext dbContext) : IDashboardService {
        public async Task<DashboardDto> GetAsync(int? year = null, int? month = null) {
            var now = DateTime.Now;
            var monthStart = new DateTime(year ?? now.Year, month ?? now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var previousMonthStart = monthStart.AddMonths(-1);
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

            var salesInvoices = await dbContext.SalesInvoices
                .AsNoTracking()
                .Include(x => x.SalesInvoiceItems).ThenInclude(x => x.Product)
                .Include(x => x.Customer)
                .Include(x => x.Debt)
                .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
                .ToListAsync();

            var previousSalesInvoices = await dbContext.SalesInvoices
                .AsNoTracking()
                .Include(x => x.SalesInvoiceItems).ThenInclude(x => x.Product)
                .Include(x => x.Debt)
                .Where(x => x.Date >= previousMonthStart && x.Date < monthStart)
                .ToListAsync();

            var purchaseInvoices = await dbContext.PurchaseInvoices
                .AsNoTracking()
                .Include(x => x.PurchaseInvoiceItems).ThenInclude(x => x.Product)
                .Include(x => x.Supplier)
                .Include(x => x.Debt)
                .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
                .ToListAsync();

            var previousPurchaseInvoices = await dbContext.PurchaseInvoices
                .AsNoTracking()
                .Include(x => x.PurchaseInvoiceItems).ThenInclude(x => x.Product)
                .Include(x => x.Debt)
                .Where(x => x.Date >= previousMonthStart && x.Date < monthStart)
                .ToListAsync();

            var expenses = await dbContext.Expenses
                .AsNoTracking()
                .Include(x => x.ExpenseType)
                .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
                .ToListAsync();

            var previousExpenses = await dbContext.Expenses
                .AsNoTracking()
                .Where(x => x.Date >= previousMonthStart && x.Date < monthStart)
                .ToListAsync();

            var cashTotal = await dbContext.MoneyAccounts
                .AsNoTracking()
                .SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;

            var customerIds = await dbContext.Customers
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();

            var supplierIds = await dbContext.Suppliers
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();

            var customerIdSet = customerIds.ToHashSet();
            var supplierIdSet = supplierIds.ToHashSet();

            var openDebts = await dbContext.Debts
                .AsNoTracking()
                .Include(x => x.Person)
                .Where(x => !x.IsDeleted && x.Amount > 0)
                .ToListAsync();

            var inventoryList = await dbContext.Inventory
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.Product.MinStock.HasValue)
                .ToListAsync();

            var receiptVouchers = await dbContext.ReceiptVouchers
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Person)
                .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
                .ToListAsync();

            var paymentVouchers = await dbContext.PaymentVouchers
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
                .ToListAsync();

            var salesItems = await dbContext.SalesInvoiceItems
                .AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.SalesInvoice)
                .Where(x => x.SalesInvoice.Date >= monthStart && x.SalesInvoice.Date < nextMonthStart)
                .ToListAsync();

            var salesCurrent = salesInvoices.Sum(GetSalesInvoiceTotal);
            var salesPrevious = previousSalesInvoices.Sum(GetSalesInvoiceTotal);

            var purchasesCurrent = purchaseInvoices.Sum(GetPurchaseInvoiceTotal);
            var purchasesPrevious = previousPurchaseInvoices.Sum(GetPurchaseInvoiceTotal);

            var expensesCurrent = expenses.Sum(x => x.Amount);
            var expensesPreviousTotal = previousExpenses.Sum(x => x.Amount);

            var costCurrent = salesInvoices.Sum(x => x.SalesInvoiceItems.Sum(i => i.CostTotal ?? i.Quantity * i.Product.CostPrice));
            var costPrevious = previousSalesInvoices.Sum(x => x.SalesInvoiceItems.Sum(i => i.CostTotal ?? i.Quantity * i.Product.CostPrice));
            var earningsCurrent = salesCurrent - costCurrent - expensesCurrent;
            var earningsPrevious = salesPrevious - costPrevious - expensesPreviousTotal;

            var lowStockCount = inventoryList.Count(x => x.Product.MinStock.HasValue && x.Quantity <= x.Product.MinStock.Value);
            var customerDebtsCount = openDebts.Count(x => x.PersonId.HasValue && customerIdSet.Contains(x.PersonId.Value));
            var supplierDebtsCount = openDebts.Count(x => x.PersonId.HasValue && supplierIdSet.Contains(x.PersonId.Value));
            var personDebtsCount = openDebts.Count(x => x.PersonId.HasValue && !customerIdSet.Contains(x.PersonId.Value) && !supplierIdSet.Contains(x.PersonId.Value));

            var dailyLabels = Enumerable.Range(1, daysInMonth).Select(x => x.ToString()).ToList();
            var dailySales = Enumerable.Repeat(0m, daysInMonth).ToList();
            var dailyPurchases = Enumerable.Repeat(0m, daysInMonth).ToList();
            var dailyProfit = Enumerable.Repeat(0m, daysInMonth).ToList();

            foreach (var invoice in salesInvoices) {
                var index = invoice.Date.Day - 1;
                if (index >= 0 && index < daysInMonth) {
                    dailySales[index] += GetSalesInvoiceTotal(invoice);
                    dailyProfit[index] += GetSalesInvoiceTotal(invoice) - invoice.SalesInvoiceItems.Sum(i => i.CostTotal ?? i.Quantity * i.Product.CostPrice);
                }
            }

            foreach (var invoice in purchaseInvoices) {
                var index = invoice.Date.Day - 1;
                if (index >= 0 && index < daysInMonth) {
                    dailyPurchases[index] += GetPurchaseInvoiceTotal(invoice);
                }
            }

            foreach (var expense in expenses) {
                var index = expense.Date.Day - 1;
                if (index >= 0 && index < daysInMonth) {
                    dailyProfit[index] -= expense.Amount;
                }
            }

            var brandSales = salesItems
                .Where(x => x.Product.Brand != null)
                .GroupBy(x => x.Product.Brand!.Name)
                .Select(g => new DashboardBrandSalesDto {
                    Name = g.Key,
                    Value = g.Sum(x => x.Quantity * (x.UnitPrice ?? x.Product.SalePrice))
                })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToList();

            var expenseBreakdown = expenses
                .GroupBy(x => x.ExpenseType.Name)
                .Select(g => new DashboardExpenseBreakdownDto {
                    Name = g.Key,
                    Value = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            var activity = BuildRecentActivity(salesInvoices, purchaseInvoices, receiptVouchers, paymentVouchers, expenses)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Reference)
                .Take(10)
                .ToList();

            return new DashboardDto {
                SalesRevenue = salesCurrent,
                CostOfGoodsSold = costCurrent,
                GrossProfit = salesCurrent - costCurrent,
                ExpenseTotal = expensesCurrent,
                NetProfit = earningsCurrent,
                HasEstimatedProfit = salesInvoices.Any(x => x.SalesInvoiceItems.Any(i => i.IsPriceEstimated || i.IsCostEstimated || i.UnitPrice == null || i.CostTotal == null)),
                MonthLabel = monthStart.ToString("MMMM yyyy"),
                Kpis = new DashboardKpisDto {
                    Sales = new DashboardKpiItemDto {
                        Value = salesCurrent,
                        DeltaPct = CalculatePercent(salesCurrent, salesPrevious)
                    },
                    Purchases = new DashboardKpiItemDto {
                        Value = purchasesCurrent,
                        DeltaPct = CalculatePercent(purchasesCurrent, purchasesPrevious)
                    },
                    Earnings = new DashboardKpiItemDto {
                        Value = earningsCurrent,
                        DeltaPct = CalculatePercent(earningsCurrent, earningsPrevious)
                    },
                    Cash = new DashboardCashKpiDto {
                        Value = cashTotal
                    }
                },
                Daily = new DashboardDailySeriesDto {
                    Labels = dailyLabels,
                    Sales = dailySales,
                    Purchases = dailyPurchases
                },
                Profit = new DashboardProfitTrendDto {
                    Labels = dailyLabels,
                    Values = dailyProfit
                },
                Brands = brandSales,
                Expenses = expenseBreakdown,
                Alerts = new List<DashboardAlertDto> {
                    new() {
                        Icon = "bi-archive",
                        Title = "Low stock items",
                        Description = $"{lowStockCount} item(s) below the minimum stock.",
                        Tone = lowStockCount > 0 ? "danger" : "info",
                        NavigateUrl = "../Inventory/index.html?lowStock=true",
                        Count = lowStockCount
                    },
                    new() {
                        Icon = "bi-people",
                        Title = "Customers debts",
                        Description = $"{customerDebtsCount} open customer debt(s).",
                        Tone = customerDebtsCount > 0 ? "warning" : "info",
                        NavigateUrl = "../Debts/index.html?partyType=Customer",
                        Count = customerDebtsCount
                    },
                    new() {
                        Icon = "bi-person-badge",
                        Title = "Persons debts",
                        Description = $"{personDebtsCount} open person debt(s).",
                        Tone = personDebtsCount > 0 ? "warning" : "info",
                        NavigateUrl = "../Debts/index.html?partyType=Person",
                        Count = personDebtsCount
                    },
                    new() {
                        Icon = "bi-truck",
                        Title = "Suppliers debts",
                        Description = $"{supplierDebtsCount} open supplier debt(s).",
                        Tone = supplierDebtsCount > 0 ? "warning" : "info",
                        NavigateUrl = "../Debts/index.html?partyType=Supplier",
                        Count = supplierDebtsCount
                    }
                },
                Activity = activity
            };
        }

        private static decimal GetSalesInvoiceTotal(Domain.Entities.SalesInvoice salesInvoice) {
            return salesInvoice.SalesInvoiceItems.Sum(x => x.Quantity * (x.UnitPrice ?? x.Product.SalePrice));
        }

        private static decimal GetPurchaseInvoiceTotal(Domain.Entities.PurchaseInvoice purchaseInvoice) {
            return purchaseInvoice.PurchaseInvoiceItems.Sum(x => x.Quantity * (x.UnitCost ?? x.Product.CostPrice));
        }

        private static decimal CalculatePercent(decimal current, decimal previous) {
            if (previous == 0m) {
                if (current == 0m) return 0m;
                return 100m;
            }

            return Math.Round(((current - previous) / Math.Abs(previous)) * 100m, 1);
        }

        private static IEnumerable<DashboardRecentActivityDto> BuildRecentActivity(
            IEnumerable<Domain.Entities.SalesInvoice> salesInvoices,
            IEnumerable<Domain.Entities.PurchaseInvoice> purchaseInvoices,
            IEnumerable<Domain.Entities.ReceiptVoucher> receiptVouchers,
            IEnumerable<Domain.Entities.PaymentVoucher> paymentVouchers,
            IEnumerable<Domain.Entities.Expense> expenses) {

            var sales = salesInvoices.Select(x => new DashboardRecentActivityDto {
                Date = x.Date,
                Type = "Sale Invoice",
                Reference = $"#SI-{x.ID}",
                Party = x.Customer?.Name ?? "Walk-in customer",
                Amount = GetSalesInvoiceTotal(x),
                Status = x.Debt != null && !x.Debt.IsDeleted && x.Debt.Amount > 0 ? "Unpaid" : "Paid",
                ViewUrl = $"../SalesInvoices/view.html?id={x.ID}"
            });

            var purchases = purchaseInvoices.Select(x => new DashboardRecentActivityDto {
                Date = x.Date,
                Type = "Purchase Invoice",
                Reference = $"#PI-{x.ID}",
                Party = x.Supplier?.Name ?? "Supplier",
                Amount = GetPurchaseInvoiceTotal(x),
                Status = x.Debt != null && !x.Debt.IsDeleted && x.Debt.Amount > 0 ? "Unpaid" : "Paid",
                ViewUrl = $"../PurchaseInvoices/view.html?id={x.ID}"
            });

            var receipts = receiptVouchers.Select(x => new DashboardRecentActivityDto {
                Date = x.Date,
                Type = "Receipt Voucher",
                Reference = $"#RV-{x.ID}",
                Party = x.Customer?.Name ?? x.Person?.Name ?? "Person",
                Amount = x.Amount,
                Status = "Received",
                ViewUrl = $"../ReceiptVouchers/view.html?id={x.ID}"
            });

            var payments = paymentVouchers.Select(x => new DashboardRecentActivityDto {
                Date = x.Date,
                Type = "Payment Voucher",
                Reference = $"#PV-{x.ID}",
                Party = x.Supplier?.Name ?? "Supplier",
                Amount = x.Amount,
                Status = "Paid",
                ViewUrl = $"../PaymentVouchers/view.html?id={x.ID}"
            });

            var highExpenses = expenses
                .Where(x => x.Amount > 100m)
                .Select(x => new DashboardRecentActivityDto {
                    Date = x.Date,
                    Type = "Expense",
                    Reference = $"#EX-{x.ID}",
                    Party = x.ExpenseType?.Name ?? "Expense",
                    Amount = x.Amount,
                    Status = "Booked",
                    ViewUrl = $"../Expenses/view.html?id={x.ID}"
                });

            return sales.Concat(purchases).Concat(receipts).Concat(payments).Concat(highExpenses);
        }
    }
}
