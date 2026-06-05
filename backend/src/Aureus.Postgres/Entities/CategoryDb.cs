namespace Aureus.Postgres.Entities;

public sealed class CategoryDb
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
