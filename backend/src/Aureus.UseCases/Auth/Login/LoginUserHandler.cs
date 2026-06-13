using Aureus.Domain.Users;
using Aureus.Infrastructure.Security.Interfaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Auth.Login;

public sealed class LoginUserHandler(
    IUserRepository userRepository,
    IEmailVerificationCodeRepository codeRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    private const string RegistrationPurpose = nameof(EmailVerificationPurpose.Registration);

    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();

        var user = await userRepository.FindByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash))
        {
            // Check if there's a pending registration so the frontend can redirect to verify screen
            if (user is null)
            {
                var pending = await codeRepository.FindAsync(email, RegistrationPurpose, cancellationToken);
                if (pending is not null)
                {
                    throw new LoginException(LoginErrorCode.EmailNotConfirmed, "Email is not confirmed.");
                }
            }

            throw new LoginException(LoginErrorCode.InvalidCredentials, "Invalid email or password.");
        }

        var token = jwtTokenGenerator.Generate(user.Id, user.Email);

        return new LoginUserResult(token);
    }
}
