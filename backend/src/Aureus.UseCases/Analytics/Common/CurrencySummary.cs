namespace Aureus.UseCases.Analytics.Common;

public sealed record CurrencySummary(string Currency, long IncomeMinor, long ExpensesMinor)
{
    public long NetMinor => IncomeMinor - ExpensesMinor;
}
