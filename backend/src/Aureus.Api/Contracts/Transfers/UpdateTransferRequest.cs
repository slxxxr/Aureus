namespace Aureus.Api.Contracts.Transfers;

public sealed record UpdateTransferRequest(
    Guid? FromAccountId,
    Guid? ToAccountId,
    long? AmountMinor,
    DateOnly? OccurredAt,
    string? Note);
