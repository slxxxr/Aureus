namespace Aureus.Persistence.Interfaces;

public enum DailyUsageFeature
{
    Insights
}

public interface IWorkspaceDailyUsageRepository
{
    Task<int> IncrementAndGetAsync(Guid workspaceId, DailyUsageFeature feature, DateOnly date, CancellationToken cancellationToken);
}
