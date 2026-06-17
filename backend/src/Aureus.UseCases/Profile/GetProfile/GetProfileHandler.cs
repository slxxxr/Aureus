using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Profile.GetProfile;

public sealed class GetProfileHandler(IUserRepository userRepository)
    : IRequestHandler<GetProfileQuery, GetProfileResult>
{
    public async Task<GetProfileResult> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(query.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {query.UserId} not found in database.");

        return new GetProfileResult(user.Id, user.Email, user.Name);
    }
}
