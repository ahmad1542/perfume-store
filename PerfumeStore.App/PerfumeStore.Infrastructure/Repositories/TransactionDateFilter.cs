using Microsoft.EntityFrameworkCore;

namespace PerfumeStore.Infrastructure.Repositories;

internal static class TransactionDateFilter {
    public static IQueryable<T> WithinDates<T>(this IQueryable<T> query, DateTime? fromDate, DateTime? toDate) where T : class {
        if (fromDate.HasValue) {
            var start = fromDate.Value.Date;
            query = query.Where(x => EF.Property<DateTime>(x, "Date") >= start);
        }
        // A date filter includes the entire selected end day, not only midnight.
        if (toDate.HasValue && toDate.Value.Date < DateTime.MaxValue.Date) {
            var end = toDate.Value.Date.AddDays(1);
            query = query.Where(x => EF.Property<DateTime>(x, "Date") < end);
        }
        return query;
    }
}
