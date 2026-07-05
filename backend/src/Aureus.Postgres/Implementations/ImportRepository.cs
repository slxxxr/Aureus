using Aureus.Domain.Transactions;
using Aureus.Domain.Transfers;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

public sealed class ImportRepository(AureusDbContext dbContext, IMapper mapper) : IImportRepository
{
    public async Task AddBulkAsync(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, long> transactionAccountDeltas,
        IReadOnlyList<Transfer> transfers,
        IReadOnlyDictionary<Guid, long> transferAccountDeltas,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (transactions.Count > 0)
        {
            var transactionEntities = transactions.Select(mapper.Map<TransactionDb>).ToList();
            await dbContext.Transactions.AddRangeAsync(transactionEntities, cancellationToken);
        }

        if (transfers.Count > 0)
        {
            var transferEntities = transfers.Select(mapper.Map<TransferDb>).ToList();
            await dbContext.Transfers.AddRangeAsync(transferEntities, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var combinedDeltas = new Dictionary<Guid, long>();
        foreach (var (accountId, delta) in transactionAccountDeltas)
        {
            combinedDeltas[accountId] = combinedDeltas.GetValueOrDefault(accountId) + delta;
        }

        foreach (var (accountId, delta) in transferAccountDeltas)
        {
            combinedDeltas[accountId] = combinedDeltas.GetValueOrDefault(accountId) + delta;
        }

        foreach (var (accountId, delta) in combinedDeltas)
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
}
