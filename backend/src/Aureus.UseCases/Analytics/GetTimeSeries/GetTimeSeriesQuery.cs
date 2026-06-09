using Aureus.UseCases.Analytics.Common;
using MediatR;

namespace Aureus.UseCases.Analytics.GetTimeSeries;

public sealed record GetTimeSeriesQuery(AnalyticsFilter Filter, TimeInterval Interval)
    : IRequest<IReadOnlyList<TimeSeriesPoint>>;
