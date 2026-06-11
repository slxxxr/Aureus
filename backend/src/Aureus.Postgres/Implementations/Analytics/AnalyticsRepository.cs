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

        return dimension switch
        {
            BreakdownDimension.Category => await CategoryBreakdownAsync(filtered, cancellationToken),
            BreakdownDimension.Account => await AccountBreakdownAsync(filtered, cancellationToken),
            _ => await NameBreakdownAsync(filtered, cancellationToken),
        };
    }

    // Entity keys are stringified after materialization so every dimension shares a string Key — the name
    // dimension groups by the raw name (already trimmed on write) and has no id to expose. The left join keeps
    // transactions whose category was soft-deleted (rows survive the delete): null label, caller renders a fallback.
    private async Task<IReadOnlyList<BreakdownItem>> CategoryBreakdownAsync(
        IQueryable<TransactionDb> filtered, CancellationToken cancellationToken)
    {
        var rows = await (
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
            select new
            {
                grouped.Key.Key,
                grouped.Key.Label,
                grouped.Key.Currency,
                AmountMinor = grouped.Sum(transaction => transaction.AmountMinor),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new BreakdownItem(row.Key.ToString(), row.Label, row.Currency, row.AmountMinor))
            .ToList();
    }

    private async Task<IReadOnlyList<BreakdownItem>> AccountBreakdownAsync(
        IQueryable<TransactionDb> filtered, CancellationToken cancellationToken)
    {
        var rows = await (
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
            select new
            {
                grouped.Key.Key,
                grouped.Key.Label,
                grouped.Key.Currency,
                AmountMinor = grouped.Sum(transaction => transaction.AmountMinor),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new BreakdownItem(row.Key.ToString(), row.Label, row.Currency, row.AmountMinor))
            .ToList();
    }

    private async Task<IReadOnlyList<BreakdownItem>> NameBreakdownAsync(
        IQueryable<TransactionDb> filtered, CancellationToken cancellationToken) =>
        await filtered
            .GroupBy(transaction => new { transaction.Name, transaction.Currency })
            .Select(grouped => new BreakdownItem(
                grouped.Key.Name,
                grouped.Key.Name,
                grouped.Key.Currency,
                grouped.Sum(transaction => transaction.AmountMinor)))
            .ToListAsync(cancellationToken);

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
                transaction.OccurredAt,
                transaction.Currency,
            })
            .Select(group => new
            {
                group.Key.OccurredAt,
                group.Key.Currency,
                IncomeMinor = group.Sum(transaction => transaction.Type == income ? transaction.AmountMinor : 0L),
                ExpensesMinor = group.Sum(transaction => transaction.Type == expense ? transaction.AmountMinor : 0L),
            })
            .ToListAsync(cancellationToken);

        return daily
            .GroupBy(row => new
            {
                PeriodStart = BucketStart(row.OccurredAt, interval),
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

    public async Task<IReadOnlyList<CategoryTimeSeriesPoint>> GetCategoryTimeSeriesAsync(
        AnalyticsFilter filter, TimeInterval interval, CancellationToken cancellationToken)
    {
        // Same day-in-SQL / roll-up-in-memory shape as GetTimeSeriesAsync, with category as an extra key.
        // Left join keeps transactions whose category was soft-deleted (null label, caller renders a fallback).
        var daily = await (
            from transaction in ApplyFilter(dbContext.Transactions, filter)
            join category in dbContext.Categories
                on transaction.CategoryId equals category.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            group transaction by new
            {
                transaction.OccurredAt,
                transaction.Currency,
                transaction.CategoryId,
                Label = category != null ? category.Name : null,
            }
            into grouped
            select new
            {
                grouped.Key.OccurredAt,
                grouped.Key.Currency,
                grouped.Key.CategoryId,
                grouped.Key.Label,
                AmountMinor = grouped.Sum(transaction => transaction.AmountMinor),
            })
            .ToListAsync(cancellationToken);

        return daily
            .GroupBy(row => new
            {
                PeriodStart = BucketStart(row.OccurredAt, interval),
                row.Currency,
                row.CategoryId,
                row.Label,
            })
            .Select(group => new CategoryTimeSeriesPoint(
                group.Key.PeriodStart,
                group.Key.Currency,
                group.Key.CategoryId,
                group.Key.Label,
                group.Sum(row => row.AmountMinor)))
            .OrderBy(point => point.PeriodStart)
            .ThenBy(point => point.Currency)
            .ToList();
    }

    private static DateOnly BucketStart(DateOnly day, TimeInterval interval) => interval switch
    {
        TimeInterval.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        TimeInterval.Month => new DateOnly(day.Year, day.Month, 1),
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

        if (filter.CategoryIds is { Count: > 0 })
        {
            source = source.Where(transaction => filter.CategoryIds.Contains(transaction.CategoryId));
        }

        if (filter.Type is not null)
        {
            var type = filter.Type.Value.ToString();
            source = source.Where(transaction => transaction.Type == type);
        }

        return source;
    }
}
