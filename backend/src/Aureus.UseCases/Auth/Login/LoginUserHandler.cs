using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Common.Security;
using MediatR;

namespace Aureus.UseCases.Auth.Login;

public sealed class LoginUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();

        var user = await userRepository.FindByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash))
        {
            throw new LoginException(LoginErrorCode.InvalidCredentials, "Invalid email or password.");
        }

        var token = jwtTokenGenerator.Generate(user.Id, user.Email);

        return new LoginUserResult(token);
    }
}
