namespace Aureus.Api.Contracts.Transactions;

public sealed record UpdateTransactionRequest(
    long? AmountMinor,
    Guid? CategoryId,
    DateTimeOffset? OccurredAt,
    string? Note);
