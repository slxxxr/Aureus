using MediatR;

namespace Aureus.UseCases.Workspaces.AcceptInvitation;

public sealed record AcceptInvitationCommand(Guid InvitationId, Guid UserId) : IRequest;
