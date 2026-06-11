using Aureus.Domain.Analytics;

namespace Aureus.Persistence.Interfaces;

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<CurrencySummary>> GetSummaryAsync(
        AnalyticsFilter filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<BreakdownItem>> GetBreakdownAsync(
        AnalyticsFilter filter, BreakdownDimension dimension, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimeSeriesPoint>> GetTimeSeriesAsync(
        AnalyticsFilter filter, TimeInterval interval, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryTimeSeriesPoint>> GetCategoryTimeSeriesAsync(
        AnalyticsFilter filter, TimeInterval interval, CancellationToken cancellationToken);

    Task<int> GetTransactionCountAsync(
        AnalyticsFilter filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionContext>> GetTransactionsForContextAsync(
        AnalyticsFilter filter, int limit, CancellationToken cancellationToken);
}
