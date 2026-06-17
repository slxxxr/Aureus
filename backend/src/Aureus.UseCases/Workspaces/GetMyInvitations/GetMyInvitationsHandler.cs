using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetMyInvitations;

public sealed class GetMyInvitationsHandler(
    IUserRepository userRepository,
    IWorkspaceInvitationRepository invitationRepository)
    : IRequestHandler<GetMyInvitationsQuery, IReadOnlyList<PendingInvitationSummary>>
{
    public async Task<IReadOnlyList<PendingInvitationSummary>> Handle(
        GetMyInvitationsQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {query.UserId} not found in database.");

        return await invitationRepository.GetPendingForEmailAsync(user.Email, DateTimeOffset.UtcNow, cancellationToken);
    }
}
