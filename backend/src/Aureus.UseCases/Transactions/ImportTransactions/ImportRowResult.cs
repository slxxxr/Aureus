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

internal sealed record ValidTransferImportRow(
    DateOnly OccurredAt,
    long AmountMinor,
    FinancialAccount FromAccount,
    FinancialAccount ToAccount,
    string? Note);

internal sealed record ParsedRow(
    int RowNumber,
    string Date,
    string Type,
    string Amount,
    string Account,
    string ToAccount,
    string Category,
    string Name,
    string Note,
    ValidImportRow? ValidTransaction,
    ValidTransferImportRow? ValidTransfer,
    string? ErrorCode,
    string? ErrorSubject)
{
    public bool IsValid => ValidTransaction is not null || ValidTransfer is not null;
}
