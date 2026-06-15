using MediatR;

namespace Aureus.UseCases.Auth.Register.Complete;

public sealed record CompleteRegistrationCommand(string? RegistrationToken, string? Name, string? Password)
    : IRequest<CompleteRegistrationResult>;
