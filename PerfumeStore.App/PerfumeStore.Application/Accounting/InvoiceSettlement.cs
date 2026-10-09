using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Accounting;

public static class InvoiceSettlement {
    public static async Task EnsureAsync(IProductsRepository repository, Dictionary<int, int> quantities,
        IReadOnlyDictionary<int, decimal>? prices, bool purchase, decimal paid, bool hasDebt,
        decimal? debt, IReadOnlyDictionary<int, decimal?>? savedPrices = null, decimal voucherPayments = 0m) {
        if (quantities.Count == 0) throw new BusinessRuleException("Add at least one invoice item.");
        decimal total = 0;
        foreach (var (id, quantity) in quantities) {
            if (quantity <= 0) throw new BusinessRuleException("Invoice quantities must be greater than zero.");
            var product = await repository.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Product), id.ToString());
            var saved = savedPrices != null && savedPrices.TryGetValue(id, out var value) ? value : null;
            total += quantity * InvoicePrices.Resolve(id, purchase ? product.CostPrice : product.SalePrice, prices, saved);
        }
        Ensure(total, paid, hasDebt, debt, voucherPayments);
    }

    public static void Ensure(decimal total, decimal paid, bool hasDebt, decimal? debt, decimal voucherPayments = 0m) {
        var remaining = hasDebt ? debt ?? 0m : 0m;
        if (paid < 0 || remaining < 0 || decimal.Round(paid, 2) != paid || decimal.Round(remaining, 2) != remaining)
            throw new BusinessRuleException("Payment and debt amounts must be non-negative and have at most two decimal places.");
        if (paid + remaining + voucherPayments != total)
            throw new BusinessRuleException($"Invoice line total is ₪{total:0.00}, but amount paid plus debt" +
                (voucherPayments > 0 ? " and applied voucher payments" : "") +
                $" is ₪{paid + remaining + voucherPayments:0.00}. These amounts must match. Change the unit prices, payment, or debt before saving.");
    }
}
