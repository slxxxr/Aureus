namespace Aureus.Infrastructure.Email;

public sealed record RegistrationTokenPayload(string Email, string Purpose);
