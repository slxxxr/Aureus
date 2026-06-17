namespace Aureus.Domain.Workspaces;

public sealed record WorkspaceMemberDetail(
    Guid UserId,
    string Name,
    string Email,
    WorkspaceRole Role,
    DateTimeOffset JoinedAt);
