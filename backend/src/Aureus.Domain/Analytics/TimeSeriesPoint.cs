namespace Aureus.Domain.Analytics;

public sealed record TimeSeriesPoint(DateOnly PeriodStart, string Currency, long IncomeMinor, long ExpensesMinor);
