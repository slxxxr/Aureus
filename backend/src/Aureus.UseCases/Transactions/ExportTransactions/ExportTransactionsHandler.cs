using System.Globalization;
using System.Text;
using Aureus.Persistence.Interfaces;
using CsvHelper;
using MediatR;

namespace Aureus.UseCases.Transactions.ExportTransactions;

public sealed class ExportTransactionsHandler(
    ITransactionRepository transactionRepository,
    ITransferRepository transferRepository,
    IFinancialAccountRepository accountRepository,
    ICategoryRepository categoryRepository) : IRequestHandler<ExportTransactionsQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportTransactionsQuery query, CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.GetByFilterAsync(query.Filter, cancellationToken);
        var transfers = query.Filter.Type.HasValue
            ? []
            : await transferRepository.GetByFilterAsync(query.Filter, cancellationToken);
        var accounts = await accountRepository.GetByWorkspaceIdAsync(query.Filter.WorkspaceId, cancellationToken);
        var categories = await categoryRepository.GetAllIncludingDeletedAsync(query.Filter.WorkspaceId, cancellationToken);

        var accountNames = accounts.ToDictionary(a => a.Id, a => a.Name);
        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);

        var transactionRecords = transactions.Select(t => new
        {
            OccurredAt = t.OccurredAt,
            CreatedAt = t.CreatedAt,
            Record = new TransactionCsvRecord
            {
                Date = t.OccurredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Type = t.Type.ToString(),
                Amount = (t.AmountMinor / 100m).ToString("0.00", CultureInfo.InvariantCulture),
                Currency = t.Currency,
                Account = accountNames.GetValueOrDefault(t.FinancialAccountId, t.FinancialAccountId.ToString()),
                Category = categoryNames.GetValueOrDefault(t.CategoryId, t.CategoryId.ToString()),
                Name = EscapeFormula(t.Name),
                Note = t.Note is null ? string.Empty : EscapeFormula(t.Note),
            },
        });

        var transferRecords = transfers.Select(tr => new
        {
            OccurredAt = tr.OccurredAt,
            CreatedAt = tr.CreatedAt,
            Record = new TransactionCsvRecord
            {
                Date = tr.OccurredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Type = "Transfer",
                Amount = (tr.AmountMinor / 100m).ToString("0.00", CultureInfo.InvariantCulture),
                Currency = tr.Currency,
                Account = accountNames.GetValueOrDefault(tr.FromAccountId, tr.FromAccountId.ToString()),
                ToAccount = accountNames.GetValueOrDefault(tr.ToAccountId, tr.ToAccountId.ToString()),
                Note = tr.Note is null ? string.Empty : EscapeFormula(tr.Note),
            },
        });

        var records = transactionRecords.Concat(transferRecords)
            .OrderByDescending(r => r.OccurredAt)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => r.Record);

        using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.Context.RegisterClassMap<TransactionCsvRecordMap>();
        await csv.WriteRecordsAsync(records, cancellationToken);
        await writer.FlushAsync(cancellationToken);

        return memoryStream.ToArray();
    }

    // Prefix fields that start with formula-trigger characters so spreadsheet apps don't execute them.
    private static string EscapeFormula(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
        {
            return " " + value;
        }

        return value;
    }
}
