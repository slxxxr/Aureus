using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.DeclineInvitation;

public sealed class DeclineInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IUserRepository userRepository) : IRequestHandler<DeclineInvitationCommand>
{
    public async Task Handle(DeclineInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.FindByIdAsync(command.InvitationId, cancellationToken);

        if (invitation is null)
        {
            throw new WorkspaceInvitationException(WorkspaceInvitationErrorCode.NotFound, "Invitation not found.");
        }

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {command.UserId} not found in database.");

        if (invitation.Email != user.Email)
        {
            throw new WorkspaceInvitationException(
                WorkspaceInvitationErrorCode.Forbidden, "This invitation does not belong to you.");
        }

        await invitationRepository.DeleteAsync(invitation.Id, cancellationToken);
    }
}
