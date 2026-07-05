using Aureus.Domain.Transactions;
using Aureus.Domain.Transfers;

namespace Aureus.Persistence.Interfaces;

public interface IImportRepository
{
    Task AddBulkAsync(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, long> transactionAccountDeltas,
        IReadOnlyList<Transfer> transfers,
        IReadOnlyDictionary<Guid, long> transferAccountDeltas,
        CancellationToken cancellationToken);
}
