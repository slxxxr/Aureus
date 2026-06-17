using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.TransferOwnership;

public sealed class TransferOwnershipHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<TransferOwnershipCommand>
{
    public async Task Handle(TransferOwnershipCommand command, CancellationToken cancellationToken)
    {
        if (command.ToUserId == command.FromUserId)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.CannotTargetSelf,
                "Cannot transfer ownership to yourself.");
        }

        var target = await workspaceRepository.FindMembershipAsync(
            command.WorkspaceId, command.ToUserId, cancellationToken);

        if (target is null)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.MemberNotFound,
                "The specified user is not a member of this workspace.");
        }

        await workspaceRepository.TransferOwnershipAsync(
            command.WorkspaceId, command.FromUserId, command.ToUserId, cancellationToken);
    }
}
