namespace Aureus.UseCases.Analytics.Common;

public sealed record BreakdownItem(Guid Key, string? Label, string Currency, long AmountMinor);
