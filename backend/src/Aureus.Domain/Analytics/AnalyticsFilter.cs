using Aureus.Domain.Transactions;

namespace Aureus.Domain.Analytics;

public sealed record AnalyticsFilter(
    Guid WorkspaceId,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<Guid>? AccountIds,
    TransactionType? Type,
    IReadOnlyList<Guid>? CategoryIds = null);
