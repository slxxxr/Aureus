using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(Guid WorkspaceId, string? Name) : IRequest<Workspace>;
