using Aureus.Domain.Workspaces;

namespace Aureus.Persistence.Interfaces;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<UserWorkspaceSummary>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<WorkspaceMembership?> FindMembershipAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task<Workspace?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountActiveMembersAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(Workspace workspace, WorkspaceMember member, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceMemberDetail>> GetMembersAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task DeleteMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    Task UpdateMemberRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole newRole, CancellationToken cancellationToken);

    Task TransferOwnershipAsync(Guid workspaceId, Guid fromUserId, Guid toUserId, CancellationToken cancellationToken);

    Task UpdateAsync(Workspace workspace, CancellationToken cancellationToken);

    Task DeleteAsync(Workspace workspace, CancellationToken cancellationToken);
}
