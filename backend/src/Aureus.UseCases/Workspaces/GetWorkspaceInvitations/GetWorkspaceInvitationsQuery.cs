using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetWorkspaceInvitations;

public sealed record GetWorkspaceInvitationsQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<WorkspaceInvitation>>;
