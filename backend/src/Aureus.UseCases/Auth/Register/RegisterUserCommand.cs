using MediatR;

namespace Aureus.UseCases.Auth.Register;

public sealed record RegisterUserCommand(string? Email, string? Password) : IRequest<RegisterUserResult>;
