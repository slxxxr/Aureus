using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetMyInvitations;

public sealed record GetMyInvitationsQuery(Guid UserId) : IRequest<IReadOnlyList<PendingInvitationSummary>>;
