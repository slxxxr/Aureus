using Aureus.Domain.Transactions;

namespace Aureus.UseCases.Analytics.Common;

public sealed record AnalyticsFilter(
    Guid WorkspaceId,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<Guid>? AccountIds,
    TransactionType? Type,
    IReadOnlyList<Guid>? CategoryIds = null);
