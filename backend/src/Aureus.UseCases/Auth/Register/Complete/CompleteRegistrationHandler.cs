using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Infrastructure.Security.Interfaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Auth.Register.Complete;

public sealed class CompleteRegistrationHandler(
    IRegistrationTokenService tokenService,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<CompleteRegistrationCommand, CompleteRegistrationResult>
{
    private const int MinimumPasswordLength = 8;
    private const string DefaultWorkspaceName = "Personal";
    private const string ExpectedPurpose = nameof(EmailVerificationPurpose.Registration);

    public async Task<CompleteRegistrationResult> Handle(
        CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        var payload = tokenService.TryValidate(command.RegistrationToken ?? string.Empty);

        if (payload is null || payload.Purpose != ExpectedPurpose)
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.RegistrationTokenInvalid,
                "Registration token is invalid or expired. Please start registration again.");
        }

        var email = payload.Email;
        var password = command.Password ?? string.Empty;

        if (password.Length < MinimumPasswordLength)
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.InvalidPassword,
                $"Password must be at least {MinimumPasswordLength} characters long.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            CreatedAt = now,
        };

        var workspace = new Workspace
        {
            Id = workspaceId,
            OwnerUserId = userId,
            Name = DefaultWorkspaceName,
            CreatedAt = now,
        };

        var workspaceMember = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Owner,
            JoinedAt = now,
        };

        await userRepository.AddAsync(user, workspace, workspaceMember, cancellationToken);

        var accessToken = jwtTokenGenerator.Generate(userId, email);
        return new CompleteRegistrationResult(accessToken);
    }
}
