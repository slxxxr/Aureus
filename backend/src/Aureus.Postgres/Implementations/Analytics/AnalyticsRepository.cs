using Aureus.Domain.Transactions;
using Aureus.Postgres.Entities;
using Aureus.UseCases.Analytics.Common;
using Aureus.UseCases.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations.Analytics;

public sealed class AnalyticsRepository(AureusDbContext dbContext) : IAnalyticsRepository
{
    public async Task<IReadOnlyList<CurrencySummary>> GetSummaryAsync(
        AnalyticsFilter filter, CancellationToken cancellationToken)
    {
        var income = nameof(TransactionType.Income);
        var expense = nameof(TransactionType.Expense);

        return await ApplyFilter(dbContext.Transactions, filter)
            .GroupBy(transaction => transaction.Currency)
            .Select(group => new CurrencySummary(
                group.Key,
                group.Sum(transaction => transaction.Type == income ? transaction.AmountMinor : 0L),
                group.Sum(transaction => transaction.Type == expense ? transaction.AmountMinor : 0L)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BreakdownItem>> GetBreakdownAsync(
        AnalyticsFilter filter, BreakdownDimension dimension, CancellationToken cancellationToken)
    {
        var filtered = ApplyFilter(dbContext.Transactions, filter);

        // Left joins keep transactions whose category was soft-deleted (its rows survive a category delete);
        // such groups get a null label and the caller renders a fallback.
        if (dimension == BreakdownDimension.Category)
        {
            return await (
                from transaction in filtered
                join category in dbContext.Categories
                    on transaction.CategoryId equals category.Id into categoryJoin
                from category in categoryJoin.DefaultIfEmpty()
                group transaction by new
                {
                    Key = transaction.CategoryId,
                    Label = category != null ? category.Name : null,
                    transaction.Currency,
                }
                into grouped
                select new BreakdownItem(
                    grouped.Key.Key,
                    grouped.Key.Label,
                    grouped.Key.Currency,
                    grouped.Sum(transaction => transaction.AmountMinor)))
                .ToListAsync(cancellationToken);
        }

        return await (
            from transaction in filtered
            join account in dbContext.FinancialAccounts
                on transaction.FinancialAccountId equals account.Id into accountJoin
            from account in accountJoin.DefaultIfEmpty()
            group transaction by new
            {
                Key = transaction.FinancialAccountId,
                Label = account != null ? account.Name : null,
                transaction.Currency,
            }
            into grouped
            select new BreakdownItem(
                grouped.Key.Key,
                grouped.Key.Label,
                grouped.Key.Currency,
                grouped.Sum(transaction => transaction.AmountMinor)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeSeriesPoint>> GetTimeSeriesAsync(
        AnalyticsFilter filter, TimeInterval interval, CancellationToken cancellationToken)
    {
        var income = nameof(TransactionType.Income);
        var expense = nameof(TransactionType.Expense);

        // Group by day in SQL (natively translated), then roll up to the requested interval in memory —
        // the daily set is bounded by the period filter, so the rollup stays cheap and provider-agnostic.
        var daily = await ApplyFilter(dbContext.Transactions, filter)
            .GroupBy(transaction => new
            {
                transaction.OccurredAt.Year,
                transaction.OccurredAt.Month,
                transaction.OccurredAt.Day,
                transaction.Currency,
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                group.Key.Currency,
                IncomeMinor = group.Sum(transaction => transaction.Type == income ? transaction.AmountMinor : 0L),
                ExpensesMinor = group.Sum(transaction => transaction.Type == expense ? transaction.AmountMinor : 0L),
            })
            .ToListAsync(cancellationToken);

        return daily
            .GroupBy(row => new
            {
                PeriodStart = BucketStart(new DateTimeOffset(row.Year, row.Month, row.Day, 0, 0, 0, TimeSpan.Zero), interval),
                row.Currency,
            })
            .Select(group => new TimeSeriesPoint(
                group.Key.PeriodStart,
                group.Key.Currency,
                group.Sum(row => row.IncomeMinor),
                group.Sum(row => row.ExpensesMinor)))
            .OrderBy(point => point.PeriodStart)
            .ThenBy(point => point.Currency)
            .ToList();
    }

    private static DateTimeOffset BucketStart(DateTimeOffset day, TimeInterval interval) => interval switch
    {
        TimeInterval.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        TimeInterval.Month => new DateTimeOffset(day.Year, day.Month, 1, 0, 0, 0, TimeSpan.Zero),
        _ => day,
    };

    private static IQueryable<TransactionDb> ApplyFilter(IQueryable<TransactionDb> source, AnalyticsFilter filter)
    {
        source = source.Where(transaction => transaction.WorkspaceId == filter.WorkspaceId);

        if (filter.From is not null)
        {
            source = source.Where(transaction => transaction.OccurredAt >= filter.From.Value);
        }

        if (filter.To is not null)
        {
            source = source.Where(transaction => transaction.OccurredAt < filter.To.Value);
        }

        if (filter.AccountIds is { Count: > 0 })
        {
            source = source.Where(transaction => filter.AccountIds.Contains(transaction.FinancialAccountId));
        }

        if (filter.Type is not null)
        {
            var type = filter.Type.Value.ToString();
            source = source.Where(transaction => transaction.Type == type);
        }

        return source;
    }
}
