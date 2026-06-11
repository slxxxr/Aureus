using System.Text.RegularExpressions;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Infrastructure.Security.Interfaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Auth.Register;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private const int MinimumPasswordLength = 8;
    private const string DefaultWorkspaceName = "Personal";

    private static readonly Regex _emailRegex = new(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(command.Email);

        if (!_emailRegex.IsMatch(email))
        {
            throw new RegistrationException(RegistrationErrorCode.InvalidEmail, "Email is invalid.");
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < MinimumPasswordLength)
        {
            throw new RegistrationException(
                RegistrationErrorCode.InvalidPassword,
                $"Password must be at least {MinimumPasswordLength} characters long.");
        }

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new RegistrationException(RegistrationErrorCode.EmailAlreadyExists, "Email is already registered.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHasher.Hash(command.Password),
            CreatedAt = now
        };

        var workspace = new Workspace
        {
            Id = workspaceId,
            OwnerUserId = userId,
            Name = DefaultWorkspaceName,
            CreatedAt = now
        };

        var workspaceMember = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Owner,
            JoinedAt = now
        };

        await userRepository.AddAsync(user, workspace, workspaceMember, cancellationToken);

        return new RegisterUserResult(userId, workspaceId);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
