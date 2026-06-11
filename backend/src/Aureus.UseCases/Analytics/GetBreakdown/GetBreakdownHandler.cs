using Aureus.Domain.Analytics;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Analytics.GetBreakdown;

public sealed class GetBreakdownHandler(IAnalyticsRepository repository)
    : IRequestHandler<GetBreakdownQuery, IReadOnlyList<BreakdownItem>>
{
    public Task<IReadOnlyList<BreakdownItem>> Handle(GetBreakdownQuery query, CancellationToken cancellationToken)
    {
        return repository.GetBreakdownAsync(query.Filter, query.Dimension, cancellationToken);
    }
}
