using Aureus.Domain.Analytics;
using Aureus.Domain.Transfers;

namespace Aureus.Persistence.Interfaces;

public interface ITransferRepository
{
    Task<IReadOnlyList<Transfer>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transfer>> GetByFilterAsync(AnalyticsFilter filter, CancellationToken cancellationToken);

    Task<Transfer?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(Transfer transfer, CancellationToken cancellationToken);

    Task AddBulkAsync(IReadOnlyList<Transfer> transfers, IReadOnlyDictionary<Guid, long> accountBalanceDeltas, CancellationToken cancellationToken);

    Task UpdateAsync(Transfer transfer, IReadOnlyDictionary<Guid, long> accountBalanceDeltas, CancellationToken cancellationToken);

    Task DeleteAsync(Transfer transfer, CancellationToken cancellationToken);
}
