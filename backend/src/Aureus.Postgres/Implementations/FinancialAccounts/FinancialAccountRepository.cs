using Aureus.Domain.FinancialAccounts;
using Aureus.Postgres.Entities;
using Aureus.UseCases.Common.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aureus.Postgres.Implementations.FinancialAccounts;

public sealed class FinancialAccountRepository(AureusDbContext dbContext, IMapper mapper)
    : IFinancialAccountRepository
{
    public async Task<IReadOnlyList<FinancialAccount>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<FinancialAccount>>(entities);
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
            .Where(t => t.FinancialAccountId == account.Id && !t.IsDeleted)
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
}
