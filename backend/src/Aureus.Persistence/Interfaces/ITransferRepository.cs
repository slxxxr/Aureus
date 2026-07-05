using Aureus.Domain.Transfers;

namespace Aureus.Persistence.Interfaces;

public interface ITransferRepository
{
    Task<IReadOnlyList<Transfer>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<Transfer?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(Transfer transfer, CancellationToken cancellationToken);

    Task UpdateAsync(Transfer transfer, long fromAccountDelta, long toAccountDelta, CancellationToken cancellationToken);

    Task DeleteAsync(Transfer transfer, CancellationToken cancellationToken);
}
