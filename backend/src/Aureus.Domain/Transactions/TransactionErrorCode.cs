namespace Aureus.Domain.Transactions;

public enum TransactionErrorCode
{
    NotFound = 1,
    CategoryRequiredOnTypeChange = 2,
    CategoryTypeMismatch = 3,
    AccountNotFound = 4,
    Forbidden = 5,
    ImportFileTooLarge = 6,
    ImportTooManyRows = 7,
    ImportInvalidFormat = 8,
    ImportHasErrors = 9,
}
