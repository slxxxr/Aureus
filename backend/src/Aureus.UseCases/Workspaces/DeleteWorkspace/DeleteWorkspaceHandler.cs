using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
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

        await workspaceRepository.DeleteAsync(workspace, cancellationToken);
    }
}
