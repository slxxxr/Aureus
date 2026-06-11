using Aureus.Domain.FinancialAccounts;

namespace Aureus.Persistence.Interfaces;

public interface IFinancialAccountRepository
{
    Task<IReadOnlyList<FinancialAccount>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<FinancialAccount?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(FinancialAccount account, CancellationToken cancellationToken);

    Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken);

    Task DeleteAsync(FinancialAccount account, CancellationToken cancellationToken);
}
