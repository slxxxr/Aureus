using Aureus.Persistence.Interfaces;

namespace Aureus.Persistence.Entities;

public sealed class WorkspaceDailyUsageDb
{
    public Guid WorkspaceId { get; set; }

    public DailyUsageFeature Feature { get; set; }

    public DateOnly LastDate { get; set; }

    public int Count { get; set; }
}