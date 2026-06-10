using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    Guid FinancialAccountId,
    Guid CategoryId,
    Guid CreatedByUserId,
    string Name,
    TransactionType Type,
    long AmountMinor,
    string Currency,
    DateOnly OccurredAt,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
