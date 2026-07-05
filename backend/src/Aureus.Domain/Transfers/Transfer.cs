namespace Aureus.Domain.Transfers;

public sealed class Transfer
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid FromAccountId { get; set; }

    public Guid ToAccountId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public long AmountMinor { get; set; }

    public string Currency { get; set; } = "RUB";

    public DateOnly OccurredAt { get; set; }

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
