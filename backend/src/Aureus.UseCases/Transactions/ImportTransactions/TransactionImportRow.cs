using CsvHelper.Configuration.Attributes;

namespace Aureus.UseCases.Transactions.ImportTransactions;

internal sealed class TransactionImportRow
{
    [Name("date")] public string Date { get; set; } = string.Empty;
    [Name("type")] public string Type { get; set; } = string.Empty;
    [Name("amount")] public string Amount { get; set; } = string.Empty;
    [Name("currency")] public string? Currency { get; set; }
    [Name("account")] public string Account { get; set; } = string.Empty;
    [Name("toAccount")] public string? ToAccount { get; set; }
    [Name("category")] public string Category { get; set; } = string.Empty;
    [Name("name")] public string Name { get; set; } = string.Empty;
    [Name("note")] public string? Note { get; set; }
}
