namespace Aureus.Infrastructure.Email.Interfaces;

public interface IRegistrationTokenService
{
    string Generate(string email, string purpose);

    RegistrationTokenPayload? TryValidate(string token);
}
