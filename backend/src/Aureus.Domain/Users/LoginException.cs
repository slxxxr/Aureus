using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Users;

public sealed class LoginException(LoginErrorCode code, string message) : DomainException(message)
{
    public LoginErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        LoginErrorCode.EmailNotConfirmed => ErrorType.Validation,
        _ => ErrorType.Unauthorized,
    };
}
