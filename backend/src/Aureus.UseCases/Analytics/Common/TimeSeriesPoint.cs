namespace Aureus.UseCases.Analytics.Common;

public sealed record TimeSeriesPoint(DateOnly PeriodStart, string Currency, long IncomeMinor, long ExpensesMinor);
