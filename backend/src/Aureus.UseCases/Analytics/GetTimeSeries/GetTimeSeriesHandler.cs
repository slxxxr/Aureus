using Aureus.Domain.Analytics;
using Aureus.Persistence.Interfaces;
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
