namespace Aureus.UseCases.Analytics.Common;

public sealed record BreakdownItem(string Key, string? Label, string Currency, long AmountMinor);
