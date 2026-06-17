using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetWorkspaceMembers;

public sealed class GetWorkspaceMembersHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetWorkspaceMembersQuery, IReadOnlyList<WorkspaceMemberDetail>>
{
    public Task<IReadOnlyList<WorkspaceMemberDetail>> Handle(
        GetWorkspaceMembersQuery query, CancellationToken cancellationToken) =>
        workspaceRepository.GetMembersAsync(query.WorkspaceId, cancellationToken);
}
