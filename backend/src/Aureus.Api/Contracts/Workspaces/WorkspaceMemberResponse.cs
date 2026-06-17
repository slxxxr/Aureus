using Aureus.Domain.Workspaces;

namespace Aureus.Api.Contracts.Workspaces;

public sealed record WorkspaceMemberResponse(
    Guid UserId,
    string Name,
    string Email,
    WorkspaceRole Role,
    DateTimeOffset JoinedAt);
