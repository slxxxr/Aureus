namespace Aureus.Persistence.Entities;

public sealed class TransactionDb
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid FinancialAccountId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public long AmountMinor { get; set; }

    public string Currency { get; set; } = "RUB";

    public DateOnly OccurredAt { get; set; }

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
