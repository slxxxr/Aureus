using Aureus.Domain.Analytics;
using MediatR;

namespace Aureus.UseCases.Analytics.GetSummary;

public sealed record GetSummaryQuery(AnalyticsFilter Filter) : IRequest<IReadOnlyList<CurrencySummary>>;
