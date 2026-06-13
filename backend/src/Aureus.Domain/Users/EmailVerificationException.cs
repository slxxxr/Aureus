using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Users;

public sealed class EmailVerificationException(EmailVerificationErrorCode code, string message)
    : DomainException(message)
{
    public EmailVerificationErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        EmailVerificationErrorCode.RateLimited   => ErrorType.Conflict,
        EmailVerificationErrorCode.CodeNotFound  => ErrorType.NotFound,
        EmailVerificationErrorCode.InvalidEmail  => ErrorType.Validation,
        EmailVerificationErrorCode.InvalidPassword => ErrorType.Validation,
        EmailVerificationErrorCode.RegistrationTokenInvalid => ErrorType.Unauthorized,
        EmailVerificationErrorCode.EmailAlreadyConfirmed    => ErrorType.Conflict,
        _ => ErrorType.Validation,
    };
}
