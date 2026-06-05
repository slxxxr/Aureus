using Aureus.Domain.Workspaces;
using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations.Workspaces;

public sealed class WorkspaceRepository(AureusDbContext dbContext) : IWorkspaceRepository
{
    public async Task<WorkspaceMembership?> FindMembershipAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var member = await dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .Select(m => new { m.WorkspaceId, m.UserId, m.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            return null;
        }

        return Enum.TryParse<WorkspaceRole>(member.Role, out var role)
            ? new WorkspaceMembership(member.WorkspaceId, member.UserId, role)
            : null;
    }

    public async Task<IReadOnlyList<UserWorkspaceSummary>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => new UserWorkspaceSummary(member.WorkspaceId, member.Workspace.Name, member.Role))
            .ToListAsync(cancellationToken);
    }
}
