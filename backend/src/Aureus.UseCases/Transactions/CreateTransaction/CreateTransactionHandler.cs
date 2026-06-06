using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Transactions.CreateTransaction;

public sealed class CreateTransactionHandler(
    ITransactionRepository transactionRepository,
    IFinancialAccountRepository accountRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<CreateTransactionCommand, Transaction>
{
    public async Task<Transaction> Handle(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        var account = await accountRepository.FindByIdAsync(
            command.FinancialAccountId, command.WorkspaceId, cancellationToken);

        if (account is null)
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.NotFound,
                $"Financial account {command.FinancialAccountId} not found.");
        }

        var category = await categoryRepository.FindByIdAsync(
            command.CategoryId, command.WorkspaceId, cancellationToken);

        if (category is null)
        {
            throw new CategoryException(
                CategoryErrorCode.NotFound,
                $"Category {command.CategoryId} not found.");
        }

        var balanceDelta = command.Type == TransactionType.Income ? command.AmountMinor : -command.AmountMinor;

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WorkspaceId = command.WorkspaceId,
            FinancialAccountId = command.FinancialAccountId,
            CategoryId = command.CategoryId,
            CreatedByUserId = command.CreatedByUserId,
            Name = command.Name.Trim(),
            Type = command.Type,
            AmountMinor = command.AmountMinor,
            Currency = account.Currency,
            OccurredAt = command.OccurredAt,
            Note = command.Note?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await transactionRepository.AddAsync(transaction, balanceDelta, cancellationToken);

        return transaction;
    }
}
