namespace Aureus.Api.Contracts.Analytics;

public sealed record TimeSeriesPointResponse(
    DateOnly PeriodStart,
    string Currency,
    long IncomeMinor,
    long ExpensesMinor);
