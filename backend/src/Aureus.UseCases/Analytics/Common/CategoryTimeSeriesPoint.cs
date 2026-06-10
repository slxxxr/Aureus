namespace Aureus.UseCases.Analytics.Common;

public sealed record CategoryTimeSeriesPoint(
    DateTimeOffset PeriodStart,
    string Currency,
    Guid CategoryId,
    string? Label,
    long AmountMinor);
