using PerfumeStore.Application.Accounting;
using PerfumeStore.Domain.Exceptions;
using Xunit;

namespace PerfumeStore.API.Tests;

public class InvoiceSettlementTests {
    [Theory]
    [InlineData(75, 75, false, 0)]
    [InlineData(80, 75, true, 5)]
    [InlineData(80, 0, true, 80)]
    [InlineData(0.30, 0.10, true, 0.20)]
    public void BalancedSettlementIsAccepted(decimal total, decimal paid, bool hasDebt, decimal debt) {
        InvoiceSettlement.Ensure(total, paid, hasDebt, debt);
    }

    [Theory]
    [InlineData(80, 75, false, 0)]
    [InlineData(80, 75, true, 4)]
    [InlineData(80, 85, false, 0)]
    [InlineData(80, 75, false, 5)]
    public void MismatchIsRejected(decimal total, decimal paid, bool hasDebt, decimal debt) {
        var error = Assert.Throws<BusinessRuleException>(() => InvoiceSettlement.Ensure(total, paid, hasDebt, debt));
        Assert.Contains("Invoice line total", error.Message);
    }

    [Fact]
    public void FractionalCentsAreRejected() {
        Assert.Throws<BusinessRuleException>(() => InvoiceSettlement.Ensure(10m, 9.999m, false, null));
    }
}
