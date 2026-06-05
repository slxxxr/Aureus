namespace Aureus.Infrastructure.Persistence.Entities;

public sealed class WorkspaceMemberDb
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTimeOffset JoinedAt { get; set; }
}
