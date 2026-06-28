using Aureus.Domain.Analytics;
using Aureus.Domain.Transactions;

namespace Aureus.Persistence.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetByFilterAsync(AnalyticsFilter filter, CancellationToken cancellationToken);

    Task<Transaction?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(Transaction transaction, long balanceDelta, CancellationToken cancellationToken);

    Task AddBulkAsync(IReadOnlyList<Transaction> transactions, IReadOnlyDictionary<Guid, long> accountBalanceDeltas, CancellationToken cancellationToken);

    Task UpdateAsync(Transaction transaction, Guid oldAccountId, long oldAccountDelta, long newAccountDelta, CancellationToken cancellationToken);

    Task DeleteAsync(Transaction transaction, long balanceDelta, CancellationToken cancellationToken);
}
