namespace Aureus.Api.Contracts.Auth.Login;

public sealed record LoginRequest(string? Email, string? Password);
