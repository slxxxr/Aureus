using Aureus.UseCases.Analytics.Common;
using MediatR;

namespace Aureus.UseCases.Analytics.GetCategoryTimeSeries;

public sealed record GetCategoryTimeSeriesQuery(AnalyticsFilter Filter, TimeInterval Interval)
    : IRequest<IReadOnlyList<CategoryTimeSeriesPoint>>;
