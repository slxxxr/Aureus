namespace Aureus.Api.Contracts.Analytics;

public sealed record CurrencySummaryResponse(
    string Currency,
    long IncomeMinor,
    long ExpensesMinor,
    long NetMinor);
