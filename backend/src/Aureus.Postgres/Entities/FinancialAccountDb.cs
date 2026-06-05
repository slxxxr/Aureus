namespace Aureus.Postgres.Entities;

public sealed class FinancialAccountDb
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Currency { get; set; } = "RUB";

    public long InitialBalanceMinor { get; set; }

    public long CurrentBalanceMinor { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
