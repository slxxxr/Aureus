using Aureus.Domain.Transactions;

namespace Aureus.Domain.Analytics;

public sealed record TransactionContext(
    DateOnly OccurredAt,
    string Name,
    string? CategoryLabel,
    TransactionType Type,
    long AmountMinor,
    string Currency);
