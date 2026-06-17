using MediatR;

namespace Aureus.UseCases.Workspaces.RevokeInvitation;

public sealed record RevokeInvitationCommand(Guid WorkspaceId, Guid InvitationId) : IRequest;
