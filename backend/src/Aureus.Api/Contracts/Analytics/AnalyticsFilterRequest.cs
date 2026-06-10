using Aureus.Domain.Transactions;

namespace Aureus.Api.Contracts.Analytics;

public sealed class AnalyticsFilterRequest
{
    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public Guid[]? AccountIds { get; set; }

    public Guid[]? CategoryIds { get; set; }

    public TransactionType? Type { get; set; }
}
