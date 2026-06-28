using Aureus.Domain.Transactions;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transactions.ImportTransactions;

public sealed class CommitImportHandler(
    IFinancialAccountRepository accountRepository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository) : IRequestHandler<CommitImportCommand, int>
{
    public async Task<int> Handle(CommitImportCommand command, CancellationToken cancellationToken)
    {
        if (command.FileContent.Length > TransactionCsvParser.MaxFileSizeBytes)
        {
            throw new TransactionException(TransactionErrorCode.ImportFileTooLarge, "File exceeds 5 MB.");
        }

        var accounts = await accountRepository.GetByWorkspaceIdAsync(command.WorkspaceId, cancellationToken);
        var categories = await categoryRepository.GetByWorkspaceIdAsync(command.WorkspaceId, cancellationToken);

        List<ParsedRow> parsed;
        try
        {
            parsed = TransactionCsvParser.Parse(command.FileContent, accounts, categories);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new TransactionException(TransactionErrorCode.ImportInvalidFormat, "Could not parse CSV file.");
        }

        if (parsed.Count > TransactionCsvParser.MaxRows)
        {
            throw new TransactionException(TransactionErrorCode.ImportTooManyRows, $"File contains more than {TransactionCsvParser.MaxRows} rows.");
        }

        var errors = parsed.Where(r => !r.IsValid).ToList();
        if (errors.Count > 0)
        {
            throw new TransactionException(TransactionErrorCode.ImportHasErrors, $"{errors.Count} row(s) have validation errors.");
        }

        var validRows = parsed.Select(r => r.Valid!).ToList();

        var baseTime = DateTimeOffset.UtcNow;
        var transactions = validRows.Select((row, i) => new Transaction
        {
            Id = Guid.NewGuid(),
            WorkspaceId = command.WorkspaceId,
            FinancialAccountId = row.Account.Id,
            CategoryId = row.Category.Id,
            CreatedByUserId = command.UserId,
            Name = row.Name,
            Type = row.Type,
            AmountMinor = row.AmountMinor,
            Currency = row.Account.Currency,
            OccurredAt = row.OccurredAt,
            Note = row.Note,
            CreatedAt = baseTime.AddTicks(i),
        }).ToList();

        var balanceDeltas = new Dictionary<Guid, long>();
        foreach (var tx in transactions)
        {
            var delta = tx.Type == TransactionType.Income ? tx.AmountMinor : -tx.AmountMinor;
            balanceDeltas[tx.FinancialAccountId] = balanceDeltas.GetValueOrDefault(tx.FinancialAccountId) + delta;
        }

        await transactionRepository.AddBulkAsync(transactions, balanceDeltas, cancellationToken);

        return transactions.Count;
    }
}
