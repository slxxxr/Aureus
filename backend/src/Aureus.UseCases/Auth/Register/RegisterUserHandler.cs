using System.Text.RegularExpressions;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Common.Security;
using MediatR;

namespace Aureus.UseCases.Auth.Register;

public sealed class RegisterUserHandler(
    IUserRegistrationDb registrationDb,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private const int MinimumPasswordLength = 8;
    private const string DefaultWorkspaceName = "Personal";

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(command.Email);

        if (!EmailRegex.IsMatch(email))
        {
            throw new RegistrationException(RegistrationErrorCode.InvalidEmail, "Email is invalid.");
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < MinimumPasswordLength)
        {
            throw new RegistrationException(
                RegistrationErrorCode.InvalidPassword,
                $"Password must be at least {MinimumPasswordLength} characters long.");
        }

        if (await registrationDb.EmailExistsAsync(email, cancellationToken))
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

        await registrationDb.AddAsync(user, workspace, workspaceMember, cancellationToken);

        return new RegisterUserResult(userId, workspaceId);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
