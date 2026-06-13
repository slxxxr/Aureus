namespace Aureus.Api.Contracts.Auth.Register;

public sealed record CompleteRegistrationRequest(string? RegistrationToken, string? Password);
