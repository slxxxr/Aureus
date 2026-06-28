using Aureus.Domain.Transactions;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transactions.ImportTransactions;

public sealed class ImportPreviewHandler(
    IFinancialAccountRepository accountRepository,
    ICategoryRepository categoryRepository) : IRequestHandler<ImportPreviewQuery, ImportPreviewResult>
{
    public async Task<ImportPreviewResult> Handle(ImportPreviewQuery query, CancellationToken cancellationToken)
    {
        if (query.FileContent.Length > TransactionCsvParser.MaxFileSizeBytes)
        {
            throw new TransactionException(TransactionErrorCode.ImportFileTooLarge, "File exceeds 5 MB.");
        }

        var accounts = await accountRepository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);
        var categories = await categoryRepository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);

        List<ParsedRow> parsed;
        try
        {
            parsed = TransactionCsvParser.Parse(query.FileContent, accounts, categories);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new TransactionException(TransactionErrorCode.ImportInvalidFormat, "Could not parse CSV file.");
        }

        if (parsed.Count > TransactionCsvParser.MaxRows)
        {
            throw new TransactionException(TransactionErrorCode.ImportTooManyRows, $"File contains more than {TransactionCsvParser.MaxRows} rows.");
        }

        var rows = parsed.Select(r => new ImportRowPreview(
            r.RowNumber, r.IsValid, r.ErrorCode, r.ErrorSubject,
            r.Date, r.Type, r.Amount, r.Account, r.Category, r.Name, r.Note)).ToList();

        return new ImportPreviewResult(rows, rows.Count(r => r.IsValid), rows.Count(r => !r.IsValid));
    }
}
