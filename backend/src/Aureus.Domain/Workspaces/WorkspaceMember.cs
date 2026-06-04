namespace Aureus.Domain.Workspaces;

public sealed class WorkspaceMember
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public WorkspaceRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}
