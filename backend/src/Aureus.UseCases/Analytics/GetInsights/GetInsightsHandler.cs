using Aureus.Domain.Analytics;
using Aureus.Domain.Transactions;
using Aureus.LLM;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Analytics.GetInsights;

// Prompt context strategy — three tiers based on transaction count, independent of period length.
// Timeseries granularity (medium/large) is driven by the period length, not the count.
//
// Small  (≤100 txns) — full transaction list; the LLM reasons temporally from individual entries.
// Medium (≤500 txns) — category × period matrix with top-3 transactions per cell + timeseries.
// Large  (>500 txns) — timeseries + aggregated category totals + top-50 transaction names.
//
// Timeseries granularity:  ≤31 days → Day  |  ≤92 days → Week  |  >92 days → Month
public sealed class GetInsightsHandler(
    IAnalyticsRepository analyticsRepository,
    IWorkspaceDailyUsageRepository dailyUsageRepository,
    ILlmClient llmClient) : IRequestHandler<GetInsightsQuery, string>
{
    private const int SmallTierMax = 100;
    private const int MediumTierMax = 500;
    private const int InsightsDailyLimit = 20;

    public async Task<string> Handle(GetInsightsQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var usageCount = await dailyUsageRepository.IncrementAndGetAsync(
            query.WorkspaceId, DailyUsageFeature.Insights, today, cancellationToken);

        if (usageCount > InsightsDailyLimit)
        {
            throw new AnalyticsException(AnalyticsErrorCode.DailyQuotaExceeded,
                $"Daily insights limit of {InsightsDailyLimit} requests per workspace exceeded.");
        }

        var filter = new AnalyticsFilter(
            WorkspaceId: query.WorkspaceId,
            From: query.From,
            To: query.To,
            AccountIds: null,
            Type: null);

        var count = await analyticsRepository.GetTransactionCountAsync(filter, cancellationToken);
        var tier = count switch { <= SmallTierMax => Tier.Small, <= MediumTierMax => Tier.Medium, _ => Tier.Large };
        var interval = SelectInterval(query.From, query.To);

        var context = await FetchContextAsync(filter, tier, interval, cancellationToken);
        var prompt = InsightsPromptBuilder.Build(query, tier, interval, count, context);

        try
        {
            return await llmClient.AskAsync(prompt, cancellationToken);
        }
        catch (LlmRateLimitedException)
        {
            throw new AnalyticsException(AnalyticsErrorCode.LlmTemporarilyUnavailable,
                "The AI service is temporarily unavailable. Please try again in a moment.");
        }
    }

    private static TimeInterval SelectInterval(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
        {
            return TimeInterval.Month;
        }

        var days = to.Value.DayNumber - from.Value.DayNumber;
        return days switch { <= 31 => TimeInterval.Day, <= 92 => TimeInterval.Week, _ => TimeInterval.Month };
    }

    private async Task<FinancialContext> FetchContextAsync(
        AnalyticsFilter filter, Tier tier, TimeInterval interval, CancellationToken ct)
    {
        var summary = await analyticsRepository.GetSummaryAsync(filter, ct);

        var expenseCategories = await analyticsRepository.GetBreakdownAsync(
            filter with { Type = TransactionType.Expense }, BreakdownDimension.Category, ct);

        var incomeCategories = await analyticsRepository.GetBreakdownAsync(
            filter with { Type = TransactionType.Income }, BreakdownDimension.Category, ct);

        if (tier == Tier.Large)
        {
            var timeSeries = await analyticsRepository.GetTimeSeriesAsync(filter, interval, ct);
            var names = await analyticsRepository.GetBreakdownAsync(filter, BreakdownDimension.Name, ct);
            return new FinancialContext(
                summary, expenseCategories, incomeCategories,
                Transactions: [],
                TimeSeries: timeSeries,
                TopNames: names.OrderByDescending(n => n.AmountMinor).Take(50).ToList());
        }

        var transactions = await analyticsRepository.GetTransactionsForContextAsync(filter, MediumTierMax, ct);

        if (tier == Tier.Small)
        {
            return new FinancialContext(
                summary, expenseCategories, incomeCategories,
                Transactions: transactions,
                TimeSeries: [],
                TopNames: []);
        }

        var mediumTimeSeries = await analyticsRepository.GetTimeSeriesAsync(filter, interval, ct);
        return new FinancialContext(
            summary, expenseCategories, incomeCategories,
            Transactions: transactions,
            TimeSeries: mediumTimeSeries,
            TopNames: []);
    }
}

internal enum Tier
{
    Small,
    Medium,
    Large
}

internal sealed record FinancialContext(
    IReadOnlyList<CurrencySummary> Summary,
    IReadOnlyList<BreakdownItem> ExpenseCategories,
    IReadOnlyList<BreakdownItem> IncomeCategories,
    IReadOnlyList<TransactionContext> Transactions,
    IReadOnlyList<TimeSeriesPoint> TimeSeries,
    IReadOnlyList<BreakdownItem> TopNames);