namespace Aureus.Api.Contracts.Transfers;

public sealed record UpdateTransferRequest(
    long? AmountMinor,
    DateOnly? OccurredAt,
    string? Note);
