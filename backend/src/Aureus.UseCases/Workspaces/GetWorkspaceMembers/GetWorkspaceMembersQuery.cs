using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Workspaces.GetWorkspaceMembers;

public sealed record GetWorkspaceMembersQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<WorkspaceMemberDetail>>;
