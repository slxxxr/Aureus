using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transfers;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transfers.CreateTransfer;

public sealed class CreateTransferHandler(
    ITransferRepository transferRepository,
    IFinancialAccountRepository accountRepository)
    : IRequestHandler<CreateTransferCommand, Transfer>
{
    public async Task<Transfer> Handle(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        var fromAccount = await accountRepository.FindByIdAsync(
            command.FromAccountId, command.WorkspaceId, cancellationToken);

        if (fromAccount is null)
        {
            throw new TransferException(
                TransferErrorCode.AccountNotFound,
                $"Financial account {command.FromAccountId} not found.");
        }

        var toAccount = await accountRepository.FindByIdAsync(
            command.ToAccountId, command.WorkspaceId, cancellationToken);

        if (toAccount is null)
        {
            throw new TransferException(
                TransferErrorCode.AccountNotFound,
                $"Financial account {command.ToAccountId} not found.");
        }

        if (fromAccount.Currency != toAccount.Currency)
        {
            throw new TransferException(
                TransferErrorCode.CurrencyMismatch,
                $"Cannot transfer between accounts with different currencies ({fromAccount.Currency} -> {toAccount.Currency}).");
        }

        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            WorkspaceId = command.WorkspaceId,
            FromAccountId = command.FromAccountId,
            ToAccountId = command.ToAccountId,
            CreatedByUserId = command.CreatedByUserId,
            AmountMinor = command.AmountMinor,
            Currency = fromAccount.Currency,
            OccurredAt = command.OccurredAt,
            Note = command.Note?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await transferRepository.AddAsync(transfer, cancellationToken);

        return transfer;
    }
}
