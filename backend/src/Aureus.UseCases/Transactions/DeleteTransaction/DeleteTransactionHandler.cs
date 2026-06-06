using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Transactions.DeleteTransaction;

public sealed class DeleteTransactionHandler(
    ITransactionRepository transactionRepository,
    IFinancialAccountRepository accountRepository)
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

        var account = await accountRepository.FindByIdAsync(
            transaction.FinancialAccountId, command.WorkspaceId, cancellationToken);

        if (account is null)
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.NotFound,
                $"Financial account {transaction.FinancialAccountId} not found.");
        }

        var effect = transaction.Type == TransactionType.Income ? transaction.AmountMinor : -transaction.AmountMinor;
        account.CurrentBalanceMinor -= effect;

        await transactionRepository.DeleteAsync(transaction, account, cancellationToken);
    }
}
