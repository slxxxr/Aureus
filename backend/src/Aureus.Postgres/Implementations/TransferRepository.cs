using Aureus.Domain.Transfers;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

public sealed class TransferRepository(AureusDbContext dbContext, IMapper mapper) : ITransferRepository
{
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
}
