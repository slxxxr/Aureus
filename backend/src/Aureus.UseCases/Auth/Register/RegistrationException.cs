namespace Aureus.UseCases.Auth.Register;

public sealed class RegistrationException(RegistrationErrorCode code, string message) : Exception(message)
{
    public RegistrationErrorCode Code { get; } = code;
}
