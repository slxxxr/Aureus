using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Transactions;

public sealed record CreateTransactionRequest(
    Guid FinancialAccountId,
    Guid CategoryId,
    string Name,
    TransactionType Type,
    long AmountMinor,
    DateTimeOffset OccurredAt,
    string? Note);
