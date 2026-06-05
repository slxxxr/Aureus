using Aureus.UseCases.Workspaces.GetUserWorkspaces;

namespace Aureus.UseCases.Common.Persistence;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<UserWorkspaceSummary>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
