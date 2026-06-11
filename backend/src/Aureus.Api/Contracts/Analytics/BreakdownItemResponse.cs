namespace Aureus.Api.Contracts.Analytics;

public sealed record BreakdownItemResponse(
    string Key,
    string? Label,
    string Currency,
    long AmountMinor);
