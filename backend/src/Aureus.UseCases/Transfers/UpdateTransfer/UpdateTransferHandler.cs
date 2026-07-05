using Aureus.Domain.Transfers;
using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transfers.UpdateTransfer;

public sealed class UpdateTransferHandler(ITransferRepository transferRepository)
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

        var oldAmount = transfer.AmountMinor;

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

        var amountDelta = transfer.AmountMinor - oldAmount;

        await transferRepository.UpdateAsync(
            transfer,
            fromAccountDelta: -amountDelta,
            toAccountDelta: amountDelta,
            cancellationToken);

        return transfer;
    }
}
