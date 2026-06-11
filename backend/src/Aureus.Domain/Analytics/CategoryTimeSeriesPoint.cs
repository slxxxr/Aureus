namespace Aureus.Domain.Analytics;

public sealed record CategoryTimeSeriesPoint(
    DateOnly PeriodStart,
    string Currency,
    Guid CategoryId,
    string? Label,
    long AmountMinor);
