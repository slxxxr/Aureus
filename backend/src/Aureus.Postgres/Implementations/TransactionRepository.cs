using Aureus.Domain.Analytics;
using Aureus.Domain.Transactions;
using Aureus.Persistence.Entities;
using Aureus.Persistence;
using Aureus.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

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
            .ThenByDescending(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Transaction>>(entities);
    }

    public async Task<IReadOnlyList<Transaction>> GetByFilterAsync(
        AnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.WorkspaceId == filter.WorkspaceId);

        if (filter.From.HasValue)
        {
            query = query.Where(t => t.OccurredAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(t => t.OccurredAt < filter.To.Value);
        }

        if (filter.Type.HasValue)
        {
            var typeString = filter.Type.Value.ToString();
            query = query.Where(t => t.Type == typeString);
        }

        if (filter.AccountIds is { Count: > 0 })
        {
            query = query.Where(t => filter.AccountIds.Contains(t.FinancialAccountId));
        }

        if (filter.CategoryIds is { Count: > 0 })
        {
            query = query.Where(t => filter.CategoryIds.Contains(t.CategoryId));
        }

        var entities = await query
            .OrderByDescending(t => t.OccurredAt)
            .ThenByDescending(t => t.CreatedAt)
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

    public async Task AddBulkAsync(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, long> accountBalanceDeltas,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entities = transactions.Select(mapper.Map<TransactionDb>).ToList();
        await dbContext.Transactions.AddRangeAsync(entities, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var (accountId, delta) in accountBalanceDeltas)
        {
            await dbContext.FinancialAccounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + delta),
                    cancellationToken);
        }

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Transaction transaction,
        Guid oldAccountId,
        long oldAccountDelta,
        long newAccountDelta,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transactions
            .Where(t => t.Id == transaction.Id)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.Name, transaction.Name)
                    .SetProperty(t => t.AmountMinor, transaction.AmountMinor)
                    .SetProperty(t => t.Type, transaction.Type.ToString())
                    .SetProperty(t => t.FinancialAccountId, transaction.FinancialAccountId)
                    .SetProperty(t => t.Currency, transaction.Currency)
                    .SetProperty(t => t.CategoryId, transaction.CategoryId)
                    .SetProperty(t => t.OccurredAt, transaction.OccurredAt)
                    .SetProperty(t => t.Note, transaction.Note)
                    .SetProperty(t => t.UpdatedAt, transaction.UpdatedAt),
                cancellationToken);

        if (oldAccountDelta != 0)
        {
            await dbContext.FinancialAccounts
                .Where(a => a.Id == oldAccountId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + oldAccountDelta),
                    cancellationToken);
        }

        if (newAccountDelta != 0)
        {
            await dbContext.FinancialAccounts
                .Where(a => a.Id == transaction.FinancialAccountId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + newAccountDelta),
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
