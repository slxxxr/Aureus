using MediatR;

namespace Aureus.UseCases.Auth.Register.Verify;

public sealed record VerifyEmailCodeCommand(string? Email, string? Code) : IRequest<VerifyEmailCodeResult>;
