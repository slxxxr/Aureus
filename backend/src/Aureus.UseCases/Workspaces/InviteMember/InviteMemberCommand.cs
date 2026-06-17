using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.InviteMember;

public sealed record InviteMemberCommand(Guid WorkspaceId, Guid InvitedByUserId, string Email, string? Language)
    : IRequest<WorkspaceInvitation>;
