using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.AcceptInvitation;

public sealed class AcceptInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository) : IRequestHandler<AcceptInvitationCommand>
{
    public async Task Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = await invitationRepository.FindByIdAsync(command.InvitationId, cancellationToken);

        if (invitation is null || invitation.ExpiresAt <= now)
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

        var activeMembers = await workspaceRepository.CountActiveMembersAsync(invitation.WorkspaceId, cancellationToken);

        if (activeMembers >= WorkspaceLimits.MaxMembers)
        {
            throw new WorkspaceInvitationException(
                WorkspaceInvitationErrorCode.WorkspaceFull,
                $"Workspace has reached the maximum of {WorkspaceLimits.MaxMembers} members.");
        }

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = invitation.WorkspaceId,
            UserId = command.UserId,
            Role = WorkspaceRole.Member,
            JoinedAt = now,
        };

        await invitationRepository.AcceptAsync(invitation.Id, member, cancellationToken);
    }
}
