namespace Aureus.Api.Contracts.Analytics;

public sealed record CategoryTimeSeriesPointResponse(
    DateOnly PeriodStart,
    string Currency,
    Guid CategoryId,
    string? Label,
    long AmountMinor);
