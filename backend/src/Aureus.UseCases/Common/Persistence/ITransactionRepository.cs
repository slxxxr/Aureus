using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;

namespace Aureus.UseCases.Common.Persistence;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<Transaction?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(Transaction transaction, FinancialAccount account, CancellationToken cancellationToken);

    Task UpdateAsync(Transaction transaction, FinancialAccount account, CancellationToken cancellationToken);

    Task DeleteAsync(Transaction transaction, FinancialAccount account, CancellationToken cancellationToken);
}
