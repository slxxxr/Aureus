using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations.Workspaces;

public sealed class WorkspaceRepository(AureusDbContext dbContext) : IWorkspaceRepository
{
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
