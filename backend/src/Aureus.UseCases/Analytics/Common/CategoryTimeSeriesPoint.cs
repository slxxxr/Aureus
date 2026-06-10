namespace Aureus.UseCases.Analytics.Common;

public sealed record CategoryTimeSeriesPoint(
    DateOnly PeriodStart,
    string Currency,
    Guid CategoryId,
    string? Label,
    long AmountMinor);
