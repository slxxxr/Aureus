using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Users;

public sealed class RegistrationException(RegistrationErrorCode code, string message) : DomainException(message)
{
    public RegistrationErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        RegistrationErrorCode.EmailAlreadyExists => ErrorType.Conflict,
        _ => ErrorType.Validation
    };
}
