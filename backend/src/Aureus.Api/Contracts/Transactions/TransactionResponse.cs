using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    Guid FinancialAccountId,
    Guid CategoryId,
    Guid CreatedByUserId,
    TransactionType Type,
    long AmountMinor,
    string Currency,
    DateTimeOffset OccurredAt,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
