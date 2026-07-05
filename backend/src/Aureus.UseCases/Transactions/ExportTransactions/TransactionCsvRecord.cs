using CsvHelper.Configuration;

namespace Aureus.UseCases.Transactions.ExportTransactions;

internal sealed class TransactionCsvRecord
{
    public string Date { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string Account { get; init; } = string.Empty;
    public string ToAccount { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
}

internal sealed class TransactionCsvRecordMap : ClassMap<TransactionCsvRecord>
{
    public TransactionCsvRecordMap()
    {
        Map(r => r.Date).Name("date").Index(0);
        Map(r => r.Type).Name("type").Index(1);
        Map(r => r.Amount).Name("amount").Index(2);
        Map(r => r.Currency).Name("currency").Index(3);
        Map(r => r.Account).Name("account").Index(4);
        Map(r => r.ToAccount).Name("toAccount").Index(5);
        Map(r => r.Category).Name("category").Index(6);
        Map(r => r.Name).Name("name").Index(7);
        Map(r => r.Note).Name("note").Index(8);
    }
}
