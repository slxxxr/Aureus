using MediatR;

namespace Aureus.UseCases.Auth.Register.Start;

public sealed record StartRegistrationCommand(string? Email) : IRequest;
