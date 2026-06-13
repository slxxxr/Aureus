using MediatR;

namespace Aureus.UseCases.Auth.Register.Complete;

public sealed record CompleteRegistrationCommand(string? RegistrationToken, string? Password)
    : IRequest<CompleteRegistrationResult>;
