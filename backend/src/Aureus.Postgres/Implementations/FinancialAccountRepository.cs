using System.Data;
using Aureus.Domain.FinancialAccounts;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aureus.Postgres.Implementations;

public sealed class FinancialAccountRepository(AureusDbContext dbContext, IMapper mapper)
    : IFinancialAccountRepository
{
    public async Task<IReadOnlyList<FinancialAccount>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entities = await GetOrderedAccounts(workspaceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return mapper.Map<List<FinancialAccount>>(entities);
    }

    public async Task ReorderAsync(
        Guid workspaceId,
        IReadOnlyList<Guid> accountIds,
        IReadOnlyList<Guid> expectedAccountIds,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        try
        {
            var accounts = await GetOrderedAccounts(workspaceId)
                .AsTracking()
                .ToListAsync(cancellationToken);

            if (!accounts.Select(account => account.Id).SequenceEqual(expectedAccountIds))
            {
                throw new FinancialAccountException(
                    FinancialAccountErrorCode.OrderConflict, "The list or order of financial accounts changed.");
            }

            var accountsById = accounts.ToDictionary(account => account.Id);
            for (var position = 0; position < accountIds.Count; position++)
            {
                accountsById[accountIds[position]].SortOrder = (short)position;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsConcurrentUpdate(exception))
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.OrderConflict, "The list or order of financial accounts changed.");
        }
    }

    public async Task<FinancialAccount?> FindByIdAsync(
        Guid id,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.WorkspaceId == workspaceId, cancellationToken);

        return entity is null ? null : mapper.Map<FinancialAccount>(entity);
    }

    public async Task AddAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<FinancialAccountDb>(account);
        dbContext.FinancialAccounts.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.NameTaken,
                $"A financial account named '{account.Name}' already exists in this workspace.");
        }
    }

    public async Task UpdateAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.FinancialAccounts
                .Where(a => a.Id == account.Id)
                .ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Name, account.Name)
                        .SetProperty(a => a.InitialBalanceMinor, account.InitialBalanceMinor)
                        .SetProperty(a => a.CurrentBalanceMinor, account.CurrentBalanceMinor)
                        .SetProperty(a => a.UpdatedAt, account.UpdatedAt),
                    cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.NameTaken,
                $"A financial account named '{account.Name}' already exists in this workspace.");
        }
    }

    public async Task DeleteAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transactions
            .Where(t => t.FinancialAccountId == account.Id)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsDeleted, true)
                    .SetProperty(t => t.DeletedAt, now),
                cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == account.Id)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.IsDeleted, true)
                    .SetProperty(a => a.DeletedAt, now),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ||
        ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } };

    private IOrderedQueryable<FinancialAccountDb> GetOrderedAccounts(Guid workspaceId) =>
        dbContext.FinancialAccounts
            .Where(account => account.WorkspaceId == workspaceId)
            .OrderBy(account => account.SortOrder == null)
            .ThenBy(account => account.SortOrder)
            .ThenBy(account => account.CreatedAt)
            .ThenBy(account => account.Id);

    private static bool IsConcurrentUpdate(Exception exception) =>
        exception.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
        };
}
