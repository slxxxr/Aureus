using Aureus.Domain.Workspaces;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Workspaces.DeleteWorkspace;

public sealed class DeleteWorkspaceHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<DeleteWorkspaceCommand>
{
    public async Task Handle(DeleteWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.FindByIdAsync(command.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            throw new WorkspaceException(WorkspaceErrorCode.NotFound, "Workspace not found.");
        }

        if (workspace.OwnerUserId != command.UserId)
        {
            throw new WorkspaceException(WorkspaceErrorCode.Forbidden, "Only the workspace owner can delete it.");
        }

        await workspaceRepository.DeleteAsync(workspace, cancellationToken);
    }
}
