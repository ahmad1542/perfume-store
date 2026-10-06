using PerfumeStore.Domain.Exceptions;

namespace PerfumeStore.Domain.Entities;

public static class InvoicePrices {
    public static bool PreserveEstimate(bool estimated, decimal? oldPrice, decimal newPrice)
        => estimated && oldPrice == newPrice;

    public static decimal Resolve(int productId, decimal defaultPrice,
        IReadOnlyDictionary<int, decimal>? prices, decimal? savedPrice = null) {
        var price = prices != null && prices.TryGetValue(productId, out var requested)
            ? requested : savedPrice ?? defaultPrice;
        if (price < 0 || decimal.Round(price, 2) != price) {
            throw new BusinessRuleException("Unit prices must be non-negative and have at most two decimal places.");
        }
        return price;
    }
}
