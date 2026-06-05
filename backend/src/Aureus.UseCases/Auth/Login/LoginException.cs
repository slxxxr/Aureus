using Aureus.UseCases.Common.Exceptions;

namespace Aureus.UseCases.Auth.Login;

public sealed class LoginException(LoginErrorCode code, string message) : UseCaseException(message)
{
    public LoginErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => ErrorType.Unauthorized;
}
