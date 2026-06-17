using Aureus.Domain.Workspaces;
using Aureus.Infrastructure;
using Aureus.Infrastructure.Email;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Persistence.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace Aureus.UseCases.Workspaces.InviteMember;

public sealed class InviteMemberHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IWorkspaceDailyUsageRepository dailyUsageRepository,
    IEmailSender emailSender,
    IOptions<AppOptions> appOptions) : IRequestHandler<InviteMemberCommand, WorkspaceInvitation>
{
    private const int DailyInviteLimit = 20;
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<WorkspaceInvitation> Handle(InviteMemberCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var language = NormalizeLanguage(command.Language);
        var now = DateTimeOffset.UtcNow;

        // Resolved up front so a missing workspace/inviter fails before the invitation is persisted.
        var workspace = await workspaceRepository.FindByIdAsync(command.WorkspaceId, cancellationToken)
            ?? throw new InvalidOperationException($"Workspace {command.WorkspaceId} not found.");
        var inviter = await userRepository.FindByIdAsync(command.InvitedByUserId, cancellationToken)
            ?? throw new InvalidOperationException($"Inviting user {command.InvitedByUserId} not found.");

        var invitedUser = await userRepository.FindByEmailAsync(email, cancellationToken);
        if (invitedUser is not null)
        {
            var membership = await workspaceRepository.FindMembershipAsync(
                command.WorkspaceId, invitedUser.Id, cancellationToken);

            if (membership is not null)
            {
                throw new WorkspaceInvitationException(
                    WorkspaceInvitationErrorCode.AlreadyMember, "This user is already a member of the workspace.");
            }
        }

        var existing = await invitationRepository.FindPendingAsync(command.WorkspaceId, email, cancellationToken);

        // A row past its expiry occupies no active slot (CountActiveAsync excludes it), so reviving
        // it via resend must go through the same cap check as a brand-new invitation.
        if (existing is null || existing.ExpiresAt <= now)
        {
            var activeMembers = await workspaceRepository.CountActiveMembersAsync(command.WorkspaceId, cancellationToken);
            var activeInvitations = await invitationRepository.CountActiveAsync(command.WorkspaceId, now, cancellationToken);

            if (activeMembers + activeInvitations >= WorkspaceLimits.MaxMembers)
            {
                throw new WorkspaceInvitationException(
                    WorkspaceInvitationErrorCode.WorkspaceFull,
                    $"Workspace has reached the maximum of {WorkspaceLimits.MaxMembers} members.");
            }
        }

        var dateToday = DateOnly.FromDateTime(now.UtcDateTime);
        var usageCount = await dailyUsageRepository.GetCountAsync(
            command.WorkspaceId, DailyUsageFeature.WorkspaceInvitations, dateToday, cancellationToken);

        if (usageCount >= DailyInviteLimit)
        {
            throw new WorkspaceInvitationException(
                WorkspaceInvitationErrorCode.DailyQuotaExceeded,
                $"Daily invitation limit of {DailyInviteLimit} requests per workspace exceeded.");
        }

        var invitation = new WorkspaceInvitation
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            WorkspaceId = command.WorkspaceId,
            Email = email,
            InvitedByUserId = command.InvitedByUserId,
            ExpiresAt = now.Add(InvitationLifetime),
            CreatedAt = existing?.CreatedAt ?? now,
        };

        await invitationRepository.UpsertAsync(invitation, cancellationToken);

        var template = InviteEmailTemplates.Build(
            language, workspace.Name, inviter.Name, invitedUser is not null, appOptions.Value.BaseUrl, invitation.ExpiresAt);

        await emailSender.SendAsync(new EmailMessage(To: email, Subject: template.Subject, HtmlBody: template.HtmlBody), cancellationToken);

        // Counted only once the email is confirmed sent, so a transient send failure doesn't burn quota.
        await dailyUsageRepository.IncrementAndGetAsync(
            command.WorkspaceId, DailyUsageFeature.WorkspaceInvitations, dateToday, cancellationToken);

        return invitation;
    }

    private static string NormalizeLanguage(string? language) =>
        (language ?? string.Empty).Trim().ToLowerInvariant() == "en" ? "en" : "ru";
}
