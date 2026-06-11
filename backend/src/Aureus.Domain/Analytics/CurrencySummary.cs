namespace Aureus.Domain.Analytics;

public sealed record CurrencySummary(string Currency, long IncomeMinor, long ExpensesMinor)
{
    public long NetMinor => IncomeMinor - ExpensesMinor;
}
