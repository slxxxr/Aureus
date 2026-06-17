using MediatR;

namespace Aureus.UseCases.Workspaces.DeclineInvitation;

public sealed record DeclineInvitationCommand(Guid InvitationId, Guid UserId) : IRequest;
