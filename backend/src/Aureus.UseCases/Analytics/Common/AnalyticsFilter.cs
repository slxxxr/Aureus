using Aureus.Domain.Transactions;

namespace Aureus.UseCases.Analytics.Common;

public sealed record AnalyticsFilter(
    Guid WorkspaceId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    IReadOnlyList<Guid>? AccountIds,
    TransactionType? Type);
