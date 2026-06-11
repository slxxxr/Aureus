using Aureus.Domain.Analytics;
using MediatR;

namespace Aureus.UseCases.Analytics.GetBreakdown;

public sealed record GetBreakdownQuery(AnalyticsFilter Filter, BreakdownDimension Dimension)
    : IRequest<IReadOnlyList<BreakdownItem>>;
