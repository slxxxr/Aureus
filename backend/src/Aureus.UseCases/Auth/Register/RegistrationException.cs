using Aureus.UseCases.Common.Exceptions;

namespace Aureus.UseCases.Auth.Register;

public sealed class RegistrationException(RegistrationErrorCode code, string message) : UseCaseException(message)
{
    public RegistrationErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        RegistrationErrorCode.EmailAlreadyExists => ErrorType.Conflict,
        _ => ErrorType.Validation
    };
}
