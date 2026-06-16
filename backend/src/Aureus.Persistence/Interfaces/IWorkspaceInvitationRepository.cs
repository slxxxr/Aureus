using Aureus.Domain.Workspaces;

namespace Aureus.Persistence.Interfaces;

public interface IWorkspaceInvitationRepository
{
    Task<WorkspaceInvitation?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<WorkspaceInvitation?> FindPendingAsync(Guid workspaceId, string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceInvitation>> GetPendingForWorkspaceAsync(
        Guid workspaceId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingInvitationSummary>> GetPendingForEmailAsync(
        string email, DateTimeOffset now, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(Guid workspaceId, DateTimeOffset now, CancellationToken cancellationToken);

    Task UpsertAsync(WorkspaceInvitation invitation, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
