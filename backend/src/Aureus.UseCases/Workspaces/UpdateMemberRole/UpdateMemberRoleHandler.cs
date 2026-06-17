using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.UpdateMemberRole;

public sealed class UpdateMemberRoleHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<UpdateMemberRoleCommand>
{
    public async Task Handle(UpdateMemberRoleCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetUserId == command.RequestingUserId)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.CannotTargetSelf,
                "Cannot change your own role. Use transfer-ownership to hand off ownership.");
        }

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
                WorkspaceMemberErrorCode.CannotChangeOwnerRole,
                "Cannot change the Owner's role. Use transfer-ownership instead.");
        }

        await workspaceRepository.UpdateMemberRoleAsync(
            command.WorkspaceId, command.TargetUserId, command.NewRole, cancellationToken);
    }
}
