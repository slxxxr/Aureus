namespace Aureus.Domain.Analytics;

public sealed record BreakdownItem(string Key, string? Label, string Currency, long AmountMinor);
