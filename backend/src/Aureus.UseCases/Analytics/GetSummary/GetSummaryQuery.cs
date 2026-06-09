using Aureus.UseCases.Analytics.Common;
using MediatR;

namespace Aureus.UseCases.Analytics.GetSummary;

public sealed record GetSummaryQuery(AnalyticsFilter Filter) : IRequest<IReadOnlyList<CurrencySummary>>;
