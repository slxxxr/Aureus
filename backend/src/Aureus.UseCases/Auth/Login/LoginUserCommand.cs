using MediatR;

namespace Aureus.UseCases.Auth.Login;

public sealed record LoginUserCommand(string? Email, string? Password) : IRequest<LoginUserResult>;
