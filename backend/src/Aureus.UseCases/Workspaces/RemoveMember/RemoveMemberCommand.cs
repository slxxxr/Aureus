using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.RemoveMember;

public sealed record RemoveMemberCommand(
    Guid WorkspaceId,
    Guid TargetUserId,
    WorkspaceRole RequestingRole) : IRequest;
