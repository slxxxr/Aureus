using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Transactions.UpdateTransaction;

public sealed class UpdateTransactionHandler(
    ITransactionRepository transactionRepository,
    IFinancialAccountRepository accountRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<UpdateTransactionCommand, Transaction>
{
    public async Task<Transaction> Handle(UpdateTransactionCommand command, CancellationToken cancellationToken)
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

        if (command.CategoryId is not null)
        {
            var category = await categoryRepository.FindByIdAsync(
                command.CategoryId.Value, command.WorkspaceId, cancellationToken);

            if (category is null)
            {
                throw new CategoryException(
                    CategoryErrorCode.NotFound,
                    $"Category {command.CategoryId} not found.");
            }

            transaction.CategoryId = command.CategoryId.Value;
        }

        if (command.AmountMinor is not null)
        {
            var oldEffect = transaction.Type == TransactionType.Income
                ? transaction.AmountMinor
                : -transaction.AmountMinor;

            var newEffect = transaction.Type == TransactionType.Income
                ? command.AmountMinor.Value
                : -command.AmountMinor.Value;

            account.CurrentBalanceMinor += newEffect - oldEffect;
            transaction.AmountMinor = command.AmountMinor.Value;
        }

        if (command.OccurredAt is not null)
        {
            transaction.OccurredAt = command.OccurredAt.Value;
        }

        if (command.Note is not null)
        {
            transaction.Note = command.Note.Trim();
        }

        transaction.UpdatedAt = DateTimeOffset.UtcNow;

        await transactionRepository.UpdateAsync(transaction, account, cancellationToken);

        return transaction;
    }
}
