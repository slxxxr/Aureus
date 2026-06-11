using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.UpdateWorkspace;

public sealed class UpdateWorkspaceHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<UpdateWorkspaceCommand, Workspace>
{
    public async Task<Workspace> Handle(UpdateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.FindByIdAsync(command.WorkspaceId, cancellationToken);

        if (workspace is null)
        {
            throw new WorkspaceException(WorkspaceErrorCode.NotFound, "Workspace not found.");
        }

        if (workspace.OwnerUserId != command.UserId)
        {
            throw new WorkspaceException(WorkspaceErrorCode.Forbidden, "Only the workspace owner can edit it.");
        }

        if (command.Name is not null)
        {
            workspace.Name = command.Name.Trim();
        }

        workspace.UpdatedAt = DateTimeOffset.UtcNow;

        await workspaceRepository.UpdateAsync(workspace, cancellationToken);

        return workspace;
    }
}
