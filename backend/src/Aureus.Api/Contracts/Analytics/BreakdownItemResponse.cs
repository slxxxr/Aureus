namespace Aureus.Api.Contracts.Analytics;

public sealed record BreakdownItemResponse(
    Guid Key,
    string? Label,
    string Currency,
    long AmountMinor);
