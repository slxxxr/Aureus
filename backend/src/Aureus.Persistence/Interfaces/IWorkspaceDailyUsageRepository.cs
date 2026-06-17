namespace Aureus.Persistence.Interfaces;

public enum DailyUsageFeature
{
    Insights,
    WorkspaceInvitations
}

public interface IWorkspaceDailyUsageRepository
{
    Task<int> GetCountAsync(Guid workspaceId, DailyUsageFeature feature, DateOnly date, CancellationToken cancellationToken);

    Task<int> IncrementAndGetAsync(Guid workspaceId, DailyUsageFeature feature, DateOnly date, CancellationToken cancellationToken);
}
