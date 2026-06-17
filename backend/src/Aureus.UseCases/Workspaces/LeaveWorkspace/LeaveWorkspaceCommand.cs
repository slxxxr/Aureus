using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.LeaveWorkspace;

public sealed record LeaveWorkspaceCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole UserRole) : IRequest;
