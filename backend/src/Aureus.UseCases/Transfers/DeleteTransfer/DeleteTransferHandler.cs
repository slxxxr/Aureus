using Aureus.Domain.Transfers;
using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transfers.DeleteTransfer;

public sealed class DeleteTransferHandler(ITransferRepository transferRepository)
    : IRequestHandler<DeleteTransferCommand>
{
    public async Task Handle(DeleteTransferCommand command, CancellationToken cancellationToken)
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
                "Members can only delete their own transfers.");
        }

        await transferRepository.DeleteAsync(transfer, cancellationToken);
    }
}
