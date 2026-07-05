using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Transfers;

public sealed class TransferException(TransferErrorCode code, string message) : DomainException(message)
{
    public TransferErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        TransferErrorCode.NotFound => ErrorType.NotFound,
        TransferErrorCode.Forbidden => ErrorType.Forbidden,
        _ => ErrorType.Validation
    };
}
