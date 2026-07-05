using Aureus.Domain.Transfers;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

public sealed class TransferRepository(AureusDbContext dbContext, IMapper mapper) : ITransferRepository
{
    public async Task<IReadOnlyList<Transfer>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.WorkspaceId == workspaceId)
            .OrderByDescending(transfer => transfer.OccurredAt)
            .ThenByDescending(transfer => transfer.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Transfer>>(entities);
    }

    public async Task<Transfer?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transfer => transfer.Id == id && transfer.WorkspaceId == workspaceId,
                cancellationToken);

        return entity is null ? null : mapper.Map<Transfer>(entity);
    }

    public async Task AddAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = mapper.Map<TransferDb>(transfer);
        dbContext.Transfers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == transfer.FromAccountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor - transfer.AmountMinor),
                cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == transfer.ToAccountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + transfer.AmountMinor),
                cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Transfer transfer,
        IReadOnlyDictionary<Guid, long> accountBalanceDeltas,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transfers
            .Where(t => t.Id == transfer.Id)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.FromAccountId, transfer.FromAccountId)
                    .SetProperty(t => t.ToAccountId, transfer.ToAccountId)
                    .SetProperty(t => t.AmountMinor, transfer.AmountMinor)
                    .SetProperty(t => t.Currency, transfer.Currency)
                    .SetProperty(t => t.OccurredAt, transfer.OccurredAt)
                    .SetProperty(t => t.Note, transfer.Note)
                    .SetProperty(t => t.UpdatedAt, transfer.UpdatedAt),
                cancellationToken);

        foreach (var (accountId, delta) in accountBalanceDeltas)
        {
            if (delta == 0)
            {
                continue;
            }

            await dbContext.FinancialAccounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + delta),
                    cancellationToken);
        }

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transfers
            .Where(t => t.Id == transfer.Id)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.IsDeleted, true)
                    .SetProperty(t => t.DeletedAt, DateTimeOffset.UtcNow),
                cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == transfer.FromAccountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor + transfer.AmountMinor),
                cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.Id == transfer.ToAccountId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.CurrentBalanceMinor, a => a.CurrentBalanceMinor - transfer.AmountMinor),
                cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }
}
