namespace Aureus.Api.Contracts.Transactions;

public sealed record UpdateTransactionRequest(
    string? Name,
    long? AmountMinor,
    Guid? CategoryId,
    DateTimeOffset? OccurredAt,
    string? Note);
