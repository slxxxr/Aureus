using Aureus.UseCases.Analytics.Common;

namespace Aureus.UseCases.Common.Persistence;

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
}
