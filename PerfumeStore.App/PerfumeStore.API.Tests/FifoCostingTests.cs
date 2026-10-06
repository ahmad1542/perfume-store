using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using Xunit;

namespace PerfumeStore.API.Tests;

public sealed class FifoCostingTests {
    private static List<InventoryBatch> NaxosStock() => [
        new() { ID = 1, ProductID = 1, OriginalQuantity = 4, UnitCost = 750, ReceivedAt = new DateTime(2026, 1, 1) },
        new() { ID = 2, ProductID = 1, OriginalQuantity = 2, UnitCost = 780, ReceivedAt = new DateTime(2026, 2, 1) }
    ];

    [Fact]
    public void SaleSpanningBatchesUsesOldCostsFirst() {
        var batches = NaxosStock();
        var sale = new SalesInvoiceItem { ID = 1, ProductID = 1, Quantity = 5, UnitPrice = 900 };
        var allocations = FifoCosting.Recalculate(batches, [sale]);
        Assert.Equal(3780m, sale.CostTotal);
        Assert.Equal(0, batches[0].RemainingQuantity);
        Assert.Equal(1, batches[1].RemainingQuantity);
        Assert.Equal(new[] { 4, 1 }, allocations.Select(a => a.Quantity));
        Assert.Equal(900m, sale.UnitPrice);
    }

    [Fact]
    public void PurchaseCostCorrectionRecalculatesCostsWithoutChangingSellingPrice() {
        var batches = NaxosStock();
        var sale = new SalesInvoiceItem { ID = 1, ProductID = 1, Quantity = 5, UnitPrice = 900 };
        FifoCosting.Recalculate(batches, [sale]);
        batches[0].UnitCost = 760;
        FifoCosting.Recalculate(batches, [sale]);
        Assert.Equal(3820m, sale.CostTotal);
        Assert.Equal(900m, sale.UnitPrice);
        Assert.Equal(1, batches.Sum(b => b.RemainingQuantity));
    }

    [Fact]
    public void DifferentCustomersCanHaveDifferentSavedPrices() {
        var batches = NaxosStock();
        var sales = new[] {
            new SalesInvoiceItem { ID = 1, ProductID = 1, Quantity = 1, UnitPrice = 30 },
            new SalesInvoiceItem { ID = 2, ProductID = 1, Quantity = 1, UnitPrice = 25 }
        };
        FifoCosting.Recalculate(batches, sales);
        Assert.Equal(30m, sales[0].UnitPrice);
        Assert.Equal(25m, sales[1].UnitPrice);
        Assert.All(sales, sale => Assert.Equal(750m, sale.CostTotal));
    }

    [Fact]
    public void CorrectionThatCannotCoverRecordedSalesIsRejected() {
        Assert.Throws<BusinessRuleException>(() => FifoCosting.Recalculate(NaxosStock(), [
            new SalesInvoiceItem { ProductID = 1, Quantity = 7 }
        ]));
    }

    [Fact]
    public void OpeningStockEstimatesRemainVisibleInSaleCost() {
        var batches = NaxosStock();
        batches[0].IsCostEstimated = true;
        var sale = new SalesInvoiceItem { ProductID = 1, Quantity = 5 };
        var allocations = FifoCosting.Recalculate(batches, [sale]);
        Assert.True(sale.IsCostEstimated);
        Assert.True(allocations[0].IsCostEstimated);
        Assert.False(allocations[1].IsCostEstimated);
    }

    [Fact]
    public void OmittedPriceKeepsSavedPriceBeforeUsingCatalogDefault() {
        Assert.Equal(25m, InvoicePrices.Resolve(1, 30, null, 25));
        Assert.Equal(30m, InvoicePrices.Resolve(1, 30, null));
        Assert.Equal(20m, InvoicePrices.Resolve(1, 30, new Dictionary<int, decimal> { [1] = 20 }, 25));
        Assert.Equal(0m, InvoicePrices.Resolve(1, 30, new Dictionary<int, decimal> { [1] = 0 }));
    }
}
