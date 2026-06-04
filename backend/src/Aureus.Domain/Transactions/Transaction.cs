namespace Aureus.Domain.Transactions;

public sealed class Transaction
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid FinancialAccountId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public TransactionType Type { get; set; }

    public long AmountMinor { get; set; }

    public string Currency { get; set; } = "RUB";

    public DateTimeOffset OccurredAt { get; set; }

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
