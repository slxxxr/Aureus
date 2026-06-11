using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetUserWorkspaces;

public sealed class GetUserWorkspacesHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetUserWorkspacesQuery, IReadOnlyList<UserWorkspaceSummary>>
{
    public Task<IReadOnlyList<UserWorkspaceSummary>> Handle(
        GetUserWorkspacesQuery query,
        CancellationToken cancellationToken)
    {
        return workspaceRepository.GetByUserIdAsync(query.UserId, cancellationToken);
    }
}
