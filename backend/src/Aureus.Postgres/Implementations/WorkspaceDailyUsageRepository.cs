using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

public sealed class WorkspaceDailyUsageRepository(AureusDbContext dbContext) : IWorkspaceDailyUsageRepository
{
    public async Task<int> IncrementAndGetAsync(Guid workspaceId, DailyUsageFeature feature, DateOnly date,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.WorkspaceDailyUsage
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Feature == feature, cancellationToken);

        if (existing is null)
        {
            dbContext.WorkspaceDailyUsage.Add(new WorkspaceDailyUsageDb
            {
                WorkspaceId = workspaceId,
                Feature = feature,
                LastDate = date,
                Count = 1
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return 1;
        }

        if (existing.LastDate != date)
        {
            existing.LastDate = date;
            existing.Count = 1;
        }
        else
        {
            existing.Count++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing.Count;
    }
}