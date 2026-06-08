using Aureus.UseCases.Analytics.Common;
using Aureus.UseCases.Common.Persistence;
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
