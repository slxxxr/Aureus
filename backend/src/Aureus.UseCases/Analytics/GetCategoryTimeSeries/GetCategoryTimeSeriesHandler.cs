using Aureus.UseCases.Analytics.Common;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Analytics.GetCategoryTimeSeries;

public sealed class GetCategoryTimeSeriesHandler(IAnalyticsRepository repository)
    : IRequestHandler<GetCategoryTimeSeriesQuery, IReadOnlyList<CategoryTimeSeriesPoint>>
{
    public Task<IReadOnlyList<CategoryTimeSeriesPoint>> Handle(
        GetCategoryTimeSeriesQuery query, CancellationToken cancellationToken)
    {
        return repository.GetCategoryTimeSeriesAsync(query.Filter, query.Interval, cancellationToken);
    }
}
