using MediatR;

namespace Aureus.UseCases.Workspaces.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(Guid WorkspaceId, Guid UserId) : IRequest;
