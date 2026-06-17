using MediatR;

namespace Aureus.UseCases.Profile.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<GetProfileResult>;
