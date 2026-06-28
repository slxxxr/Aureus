namespace Aureus.UseCases.Transactions.ImportTransactions;

internal static class ImportRowErrorCode
{
    internal const string InvalidDate = "InvalidDate";
    internal const string InvalidType = "InvalidType";
    internal const string InvalidAmount = "InvalidAmount";
    internal const string AmountTooLarge = "AmountTooLarge";
    internal const string AccountNotFound = "AccountNotFound";
    internal const string CategoryNotFound = "CategoryNotFound";
    internal const string NameRequired = "NameRequired";
    internal const string NameTooLong = "NameTooLong";
    internal const string NoteTooLong = "NoteTooLong";
}
