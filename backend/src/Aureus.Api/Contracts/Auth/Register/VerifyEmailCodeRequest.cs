namespace Aureus.Api.Contracts.Auth.Register;

public sealed record VerifyEmailCodeRequest(string? Email, string? Code);
