using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetWorkspaceInvitations;

public sealed class GetWorkspaceInvitationsHandler(IWorkspaceInvitationRepository invitationRepository)
    : IRequestHandler<GetWorkspaceInvitationsQuery, IReadOnlyList<WorkspaceInvitation>>
{
    public Task<IReadOnlyList<WorkspaceInvitation>> Handle(
        GetWorkspaceInvitationsQuery query, CancellationToken cancellationToken)
    {
        return invitationRepository.GetPendingForWorkspaceAsync(
            query.WorkspaceId, DateTimeOffset.UtcNow, cancellationToken);
    }
}
