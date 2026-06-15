using MediatR;

namespace Aureus.UseCases.Profile.UpdateProfile;

public sealed record UpdateProfileCommand(Guid UserId, string? Name) : IRequest;
