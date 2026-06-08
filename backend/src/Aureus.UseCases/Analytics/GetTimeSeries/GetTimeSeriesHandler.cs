using Aureus.UseCases.Analytics.Common;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Analytics.GetTimeSeries;

public sealed class GetTimeSeriesHandler(IAnalyticsRepository repository)
    : IRequestHandler<GetTimeSeriesQuery, IReadOnlyList<TimeSeriesPoint>>
{
    public Task<IReadOnlyList<TimeSeriesPoint>> Handle(GetTimeSeriesQuery query, CancellationToken cancellationToken)
    {
        return repository.GetTimeSeriesAsync(query.Filter, query.Interval, cancellationToken);
    }
}
