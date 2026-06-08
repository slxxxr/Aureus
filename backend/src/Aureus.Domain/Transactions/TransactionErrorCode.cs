namespace Aureus.Domain.Transactions;

public enum TransactionErrorCode
{
    NotFound = 1,
    CategoryRequiredOnTypeChange = 2,
    CategoryTypeMismatch = 3,
    AccountNotFound = 4
}
