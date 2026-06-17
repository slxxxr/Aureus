using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.RevokeInvitation;

public sealed class RevokeInvitationHandler(IWorkspaceInvitationRepository invitationRepository)
    : IRequestHandler<RevokeInvitationCommand>
{
    public async Task Handle(RevokeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.FindByIdAsync(command.InvitationId, cancellationToken);

        if (invitation is null || invitation.WorkspaceId != command.WorkspaceId)
        {
            throw new WorkspaceInvitationException(WorkspaceInvitationErrorCode.NotFound, "Invitation not found.");
        }

        await invitationRepository.DeleteAsync(invitation.Id, cancellationToken);
    }
}
