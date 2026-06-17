using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.RemoveMember;

public sealed class RemoveMemberHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        var target = await workspaceRepository.FindMembershipAsync(
            command.WorkspaceId, command.TargetUserId, cancellationToken);

        if (target is null)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.MemberNotFound,
                "The specified user is not a member of this workspace.");
        }

        if (target.Role == WorkspaceRole.Owner)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.CannotRemoveOwner,
                "The workspace Owner cannot be removed.");
        }

        if (command.RequestingRole == WorkspaceRole.Manager && target.Role >= WorkspaceRole.Manager)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.InsufficientRole,
                "Managers can only remove Members.");
        }

        await workspaceRepository.DeleteMemberAsync(
            command.WorkspaceId, command.TargetUserId, cancellationToken);
    }
}
