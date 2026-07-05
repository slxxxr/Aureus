namespace Aureus.Api.Contracts.Transfers;

public sealed record TransferResponse(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    Guid CreatedByUserId,
    long AmountMinor,
    string Currency,
    DateOnly OccurredAt,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
