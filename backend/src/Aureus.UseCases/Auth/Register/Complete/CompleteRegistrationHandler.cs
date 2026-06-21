using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Infrastructure.Security.Interfaces;
using Aureus.Persistence.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aureus.UseCases.Auth.Register.Complete;

public sealed class CompleteRegistrationHandler(
    IRegistrationTokenService tokenService,
    IRegistrationRepository registrationRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IFinancialAccountRepository accountRepository,
    ICategoryRepository categoryRepository,
    ILogger<CompleteRegistrationHandler> logger) : IRequestHandler<CompleteRegistrationCommand, CompleteRegistrationResult>
{
    private const int MinimumPasswordLength = 8;
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
        var name = (command.Name ?? string.Empty).Trim();
        var password = command.Password ?? string.Empty;

        if (password.Length < MinimumPasswordLength)
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.InvalidPassword,
                $"Password must be at least {MinimumPasswordLength} characters long.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var bundle = DefaultWorkspaceSeeds.Build(userId, command.Language, now);

        var user = new User
        {
            Id = userId,
            Email = email,
            Name = name,
            PasswordHash = passwordHasher.Hash(password),
            CreatedAt = now,
        };

        await registrationRepository.CreateUserWithWorkspaceAsync(user, bundle.Workspace, bundle.Member, cancellationToken);

        await AcceptPendingInvitationsAsync(userId, email, now, cancellationToken);

        await SeedDefaultDataAsync(bundle, cancellationToken);

        var accessToken = jwtTokenGenerator.Generate(userId, email);
        return new CompleteRegistrationResult(accessToken);
    }

    private async Task AcceptPendingInvitationsAsync(
        Guid userId, string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var pendingInvitations = await invitationRepository.GetPendingForEmailAsync(email, now, cancellationToken);

        foreach (var invitation in pendingInvitations)
        {
            var activeMembers = await workspaceRepository.CountActiveMembersAsync(invitation.WorkspaceId, cancellationToken);

            if (activeMembers >= WorkspaceLimits.MaxMembers)
            {
                continue;
            }

            var member = new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = invitation.WorkspaceId,
                UserId = userId,
                Role = WorkspaceRole.Member,
                JoinedAt = now,
            };

            await invitationRepository.AcceptAsync(invitation.Id, member, cancellationToken);
        }
    }

    private async Task SeedDefaultDataAsync(DefaultWorkspaceBundle bundle, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var account in bundle.Accounts)
            {
                await accountRepository.AddAsync(account, cancellationToken);
            }

            foreach (var category in bundle.Categories)
            {
                await categoryRepository.AddAsync(category, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to seed default data for workspace {WorkspaceId}", bundle.Workspace.Id);
        }
    }
}
