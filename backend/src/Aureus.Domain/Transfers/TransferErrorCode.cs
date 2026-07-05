namespace Aureus.Domain.Transfers;

public enum TransferErrorCode
{
    NotFound = 1,
    AccountNotFound = 2,
    CurrencyMismatch = 3,
    Forbidden = 4,
}
