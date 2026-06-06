using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.CreateWorkspace;

public sealed record CreateWorkspaceCommand(Guid UserId, string Name) : IRequest<Workspace>;
