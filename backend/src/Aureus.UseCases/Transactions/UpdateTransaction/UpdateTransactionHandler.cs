using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transactions.UpdateTransaction;

public sealed class UpdateTransactionHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IFinancialAccountRepository accountRepository)
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

        var oldAccountId = transaction.FinancialAccountId;
        var oldType = transaction.Type;
        var oldAmount = transaction.AmountMinor;

        if (command.Type is not null)
        {
            if (command.CategoryId is null)
            {
                throw new TransactionException(
                    TransactionErrorCode.CategoryRequiredOnTypeChange,
                    "CategoryId is required when changing transaction type.");
            }

            transaction.Type = command.Type.Value;
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

            if (category.Type != transaction.Type)
            {
                throw new TransactionException(
                    TransactionErrorCode.CategoryTypeMismatch,
                    $"Category type {category.Type} does not match transaction type {transaction.Type}.");
            }

            transaction.CategoryId = command.CategoryId.Value;
        }

        if (command.FinancialAccountId is not null)
        {
            var account = await accountRepository.FindByIdAsync(
                command.FinancialAccountId.Value, command.WorkspaceId, cancellationToken);

            if (account is null)
            {
                throw new TransactionException(
                    TransactionErrorCode.AccountNotFound,
                    $"Financial account {command.FinancialAccountId} not found.");
            }

            transaction.FinancialAccountId = command.FinancialAccountId.Value;
            transaction.Currency = account.Currency;
        }

        if (command.Name is not null)
        {
            transaction.Name = command.Name.Trim();
        }

        if (command.AmountMinor is not null)
        {
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

        var oldEffect = oldType == TransactionType.Income ? oldAmount : -oldAmount;
        var newEffect = transaction.Type == TransactionType.Income ? transaction.AmountMinor : -transaction.AmountMinor;

        await transactionRepository.UpdateAsync(
            transaction,
            oldAccountId,
            oldAccountDelta: -oldEffect,
            newAccountDelta: newEffect,
            cancellationToken);

        return transaction;
    }
}
