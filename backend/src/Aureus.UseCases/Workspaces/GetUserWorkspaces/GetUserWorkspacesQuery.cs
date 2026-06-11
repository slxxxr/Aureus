
using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetUserWorkspaces;

public sealed record GetUserWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyList<UserWorkspaceSummary>>;
