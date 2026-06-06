using Aureus.Domain.Transactions;
using Aureus.Postgres.Entities;
using Aureus.UseCases.Common.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations.Transactions;

public sealed class TransactionRepository(AureusDbContext dbContext, IMapper mapper) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.WorkspaceId == workspaceId)
            .OrderByDescending(transaction => transaction.OccurredAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Transaction>>(entities);
    }

    public async Task<Transaction?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transaction => transaction.Id == id && transaction.WorkspaceId == workspaceId,
                cancellationToken);

        return entity is null ? null : mapper.Map<Transaction>(entity);
    }

    public async Task AddAsync(Transaction transaction, long balanceDelta, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = mapper.Map<TransactionDb>(transaction);
        dbContext.Transactions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == transaction.FinancialAccountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + balanceDelta),
                cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, long balanceDelta, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transactions
            .Where(t => t.Id == transaction.Id)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.AmountMinor, transaction.AmountMinor)
                    .SetProperty(t => t.CategoryId, transaction.CategoryId)
                    .SetProperty(t => t.OccurredAt, transaction.OccurredAt)
                    .SetProperty(t => t.Note, transaction.Note)
                    .SetProperty(t => t.UpdatedAt, transaction.UpdatedAt),
                cancellationToken);

        if (balanceDelta != 0)
        {
            await dbContext.FinancialAccounts
                .Where(a => a.Id == transaction.FinancialAccountId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + balanceDelta),
                    cancellationToken);
        }

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Transaction transaction, long balanceDelta, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transactions
            .Where(t => t.Id == transaction.Id)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.IsDeleted, true)
                    .SetProperty(t => t.DeletedAt, DateTimeOffset.UtcNow),
                cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == transaction.FinancialAccountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + balanceDelta),
                cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }
}
