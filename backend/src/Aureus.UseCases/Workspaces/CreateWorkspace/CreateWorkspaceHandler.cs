using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<CreateWorkspaceCommand, Workspace>
{
    public async Task<Workspace> Handle(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.UserId,
            Name = command.Name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = command.UserId,
            Role = WorkspaceRole.Owner,
            JoinedAt = workspace.CreatedAt,
        };

        await workspaceRepository.AddAsync(workspace, member, cancellationToken);

        return workspace;
    }
}
