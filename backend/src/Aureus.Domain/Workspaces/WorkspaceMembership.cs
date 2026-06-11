namespace Aureus.Domain.Workspaces;

public sealed record WorkspaceMembership(Guid WorkspaceId, Guid UserId, WorkspaceRole Role);
