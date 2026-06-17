using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.LeaveWorkspace;

public sealed class LeaveWorkspaceHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<LeaveWorkspaceCommand>
{
    public async Task Handle(LeaveWorkspaceCommand command, CancellationToken cancellationToken)
    {
        if (command.UserRole == WorkspaceRole.Owner)
        {
            throw new WorkspaceMemberException(
                WorkspaceMemberErrorCode.CannotLeaveAsOwner,
                "Workspace Owners cannot leave. Transfer ownership first.");
        }

        await workspaceRepository.DeleteMemberAsync(
            command.WorkspaceId, command.UserId, cancellationToken);
    }
}
