using Aureus.Domain.Analytics;
using MediatR;

namespace Aureus.UseCases.Analytics.GetTimeSeries;

public sealed record GetTimeSeriesQuery(AnalyticsFilter Filter, TimeInterval Interval)
    : IRequest<IReadOnlyList<TimeSeriesPoint>>;
