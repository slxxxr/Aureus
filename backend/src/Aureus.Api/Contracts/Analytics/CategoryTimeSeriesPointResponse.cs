namespace Aureus.Api.Contracts.Analytics;

public sealed record CategoryTimeSeriesPointResponse(
    DateTimeOffset PeriodStart,
    string Currency,
    Guid CategoryId,
    string? Label,
    long AmountMinor);
