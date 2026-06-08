using Aureus.Domain.Transactions;


namespace Aureus.Api.Contracts.Transactions;

public sealed record UpdateTransactionRequest(
    string? Name,
    long? AmountMinor,
    Guid? CategoryId,
    Guid? FinancialAccountId,
    TransactionType? Type,
    DateTimeOffset? OccurredAt,
    string? Note);
