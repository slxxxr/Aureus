using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Profile.UpdateProfile;

public sealed class UpdateProfileHandler(IUserRepository userRepository) : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {command.UserId} not found.");

        if (command.Name is not null)
        {
            user.Name = command.Name.Trim();
        }

        await userRepository.UpdateProfileAsync(user, cancellationToken);
    }
}
