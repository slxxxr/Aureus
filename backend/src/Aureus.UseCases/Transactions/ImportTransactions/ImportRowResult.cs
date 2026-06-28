using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;

namespace Aureus.UseCases.Transactions.ImportTransactions;

internal sealed record ValidImportRow(
    DateOnly OccurredAt,
    TransactionType Type,
    long AmountMinor,
    FinancialAccount Account,
    Category Category,
    string Name,
    string? Note);

internal sealed record InvalidImportRow(string ErrorMessage);

internal sealed record ParsedRow(
    int RowNumber,
    string Date,
    string Type,
    string Amount,
    string Account,
    string Category,
    string Name,
    string Note,
    ValidImportRow? Valid,
    string? ErrorCode,
    string? ErrorSubject)
{
    public bool IsValid => Valid is not null;
}
