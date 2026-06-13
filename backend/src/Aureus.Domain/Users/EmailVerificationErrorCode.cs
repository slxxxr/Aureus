namespace Aureus.Domain.Users;

public enum EmailVerificationErrorCode
{
    InvalidEmail = 1,
    CodeNotFound = 2,
    CodeExpired = 3,
    InvalidCode = 4,
    TooManyAttempts = 5,
    RateLimited = 6,
    RegistrationTokenInvalid = 7,
    InvalidPassword = 8,
    EmailAlreadyConfirmed = 9,
}
