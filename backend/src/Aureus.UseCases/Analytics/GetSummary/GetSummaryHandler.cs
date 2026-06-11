using Aureus.Domain.Analytics;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Analytics.GetSummary;

public sealed class GetSummaryHandler(IAnalyticsRepository repository)
    : IRequestHandler<GetSummaryQuery, IReadOnlyList<CurrencySummary>>
{
    public Task<IReadOnlyList<CurrencySummary>> Handle(GetSummaryQuery query, CancellationToken cancellationToken)
    {
        return repository.GetSummaryAsync(query.Filter, cancellationToken);
    }
}
