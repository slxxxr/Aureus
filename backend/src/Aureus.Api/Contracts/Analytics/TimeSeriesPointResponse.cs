namespace Aureus.Api.Contracts.Analytics;

public sealed record TimeSeriesPointResponse(
    DateTimeOffset PeriodStart,
    string Currency,
    long IncomeMinor,
    long ExpensesMinor);
