using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Analytics;

public sealed class AnalyticsFilterRequest
{
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public Guid[]? AccountIds { get; set; }

    public TransactionType? Type { get; set; }
}
