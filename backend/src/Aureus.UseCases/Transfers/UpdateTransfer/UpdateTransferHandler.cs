using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transfers;
using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transfers.UpdateTransfer;

public sealed class UpdateTransferHandler(
    ITransferRepository transferRepository,
    IFinancialAccountRepository accountRepository)
    : IRequestHandler<UpdateTransferCommand, Transfer>
{
    public async Task<Transfer> Handle(UpdateTransferCommand command, CancellationToken cancellationToken)
    {
        var transfer = await transferRepository.FindByIdAsync(
            command.TransferId, command.WorkspaceId, cancellationToken);

        if (transfer is null)
        {
            throw new TransferException(
                TransferErrorCode.NotFound,
                $"Transfer {command.TransferId} not found.");
        }

        if (command.RequestingUserRole < WorkspaceRole.Manager &&
            transfer.CreatedByUserId != command.RequestingUserId)
        {
            throw new TransferException(
                TransferErrorCode.Forbidden,
                "Members can only edit their own transfers.");
        }

        var oldFromAccountId = transfer.FromAccountId;
        var oldToAccountId = transfer.ToAccountId;
        var oldAmount = transfer.AmountMinor;

        var newFromAccountId = command.FromAccountId ?? transfer.FromAccountId;
        var newToAccountId = command.ToAccountId ?? transfer.ToAccountId;

        if (newFromAccountId == newToAccountId)
        {
            throw new TransferException(
                TransferErrorCode.SameAccount,
                "FromAccountId must not equal ToAccountId.");
        }

        FinancialAccount? newFromAccount = null;
        if (command.FromAccountId is not null)
        {
            newFromAccount = await accountRepository.FindByIdAsync(
                command.FromAccountId.Value, command.WorkspaceId, cancellationToken);

            if (newFromAccount is null)
            {
                throw new TransferException(
                    TransferErrorCode.AccountNotFound,
                    $"Financial account {command.FromAccountId} not found.");
            }
        }

        FinancialAccount? newToAccount = null;
        if (command.ToAccountId is not null)
        {
            newToAccount = await accountRepository.FindByIdAsync(
                command.ToAccountId.Value, command.WorkspaceId, cancellationToken);

            if (newToAccount is null)
            {
                throw new TransferException(
                    TransferErrorCode.AccountNotFound,
                    $"Financial account {command.ToAccountId} not found.");
            }
        }

        // Currency of an unchanged side always equals transfer.Currency (account currency is immutable).
        var fromCurrency = newFromAccount?.Currency ?? transfer.Currency;
        var toCurrency = newToAccount?.Currency ?? transfer.Currency;

        if (fromCurrency != toCurrency)
        {
            throw new TransferException(
                TransferErrorCode.CurrencyMismatch,
                $"Cannot transfer between accounts with different currencies ({fromCurrency} -> {toCurrency}).");
        }

        transfer.FromAccountId = newFromAccountId;
        transfer.ToAccountId = newToAccountId;
        transfer.Currency = fromCurrency;

        if (command.AmountMinor is not null)
        {
            transfer.AmountMinor = command.AmountMinor.Value;
        }

        if (command.OccurredAt is not null)
        {
            transfer.OccurredAt = command.OccurredAt.Value;
        }

        if (command.Note is not null)
        {
            transfer.Note = command.Note.Trim();
        }

        transfer.UpdatedAt = DateTimeOffset.UtcNow;

        var deltas = new Dictionary<Guid, long>();
        void Apply(Guid accountId, long delta) =>
            deltas[accountId] = deltas.GetValueOrDefault(accountId) + delta;

        Apply(oldFromAccountId, oldAmount);
        Apply(oldToAccountId, -oldAmount);
        Apply(newFromAccountId, -transfer.AmountMinor);
        Apply(newToAccountId, transfer.AmountMinor);

        await transferRepository.UpdateAsync(transfer, deltas, cancellationToken);

        return transfer;
    }
}
