namespace Aureus.UseCases.FinancialAccounts.GetFinancialAccountsSummary;

public sealed record FinancialAccountsSummary(DateOnly RateDate, IReadOnlyList<FinancialAccountsSummary.Total> Totals)
{
    public sealed record Total(string Currency, decimal AmountMinor);
}
