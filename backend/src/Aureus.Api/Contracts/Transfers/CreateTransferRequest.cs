namespace Aureus.Api.Contracts.Transfers;

public sealed record CreateTransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    long AmountMinor,
    DateOnly OccurredAt,
    string? Note);
