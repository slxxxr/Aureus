using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Transactions;

public sealed class TransactionException(TransactionErrorCode code, string message) : DomainException(message)
{
    public TransactionErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        TransactionErrorCode.NotFound => ErrorType.NotFound,
        TransactionErrorCode.Forbidden => ErrorType.Forbidden,
        _ => ErrorType.Validation
    };
}
