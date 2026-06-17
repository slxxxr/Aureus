using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.UpdateMemberRole;

public sealed record UpdateMemberRoleCommand(
    Guid WorkspaceId,
    Guid RequestingUserId,
    Guid TargetUserId,
    WorkspaceRole NewRole) : IRequest;
