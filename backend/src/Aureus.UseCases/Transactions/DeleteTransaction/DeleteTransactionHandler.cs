using Aureus.Domain.Transactions;
using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transactions.DeleteTransaction;

public sealed class DeleteTransactionHandler(ITransactionRepository transactionRepository)
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand command, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.FindByIdAsync(
            command.TransactionId, command.WorkspaceId, cancellationToken);

        if (transaction is null)
        {
            throw new TransactionException(
                TransactionErrorCode.NotFound,
                $"Transaction {command.TransactionId} not found.");
        }

        if (command.RequestingUserRole < WorkspaceRole.Manager &&
            transaction.CreatedByUserId != command.RequestingUserId)
        {
            throw new TransactionException(
                TransactionErrorCode.Forbidden,
                "Members can only delete their own transactions.");
        }

        var balanceDelta = transaction.Type == TransactionType.Income
            ? -transaction.AmountMinor
            : transaction.AmountMinor;

        await transactionRepository.DeleteAsync(transaction, balanceDelta, cancellationToken);
    }
}
