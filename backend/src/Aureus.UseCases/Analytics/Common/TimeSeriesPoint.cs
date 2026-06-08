namespace Aureus.UseCases.Analytics.Common;

public sealed record TimeSeriesPoint(DateTimeOffset PeriodStart, string Currency, long IncomeMinor, long ExpensesMinor);
