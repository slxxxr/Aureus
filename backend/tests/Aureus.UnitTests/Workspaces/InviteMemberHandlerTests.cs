using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Infrastructure;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.InviteMember;
using Microsoft.Extensions.Options;

namespace Aureus.UnitTests.Workspaces;

public sealed class InviteMemberHandlerTests
{
    private static readonly IOptions<AppOptions> AppOptions =
        Options.Create(new AppOptions { BaseUrl = "https://aureus.life" });

    private static Workspace DefaultWorkspace(Guid id) => new()
    {
        Id = id,
        Name = "Personal",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static User DefaultInviter(Guid id) => new()
    {
        Id = id,
        Email = "inviter@test.local",
        Name = "Inviter Name",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static InviteMemberHandler BuildHandler(
        WorkspaceRepositoryMock workspaceRepo,
        WorkspaceInvitationRepositoryMock invitationRepo,
        UserRepositoryMock userRepo,
        WorkspaceDailyUsageRepositoryMock dailyUsageRepo,
        EmailSenderMock emailSender) =>
        new(workspaceRepo.Object, invitationRepo.Object, userRepo.Object, dailyUsageRepo.Object,
            emailSender.Object, AppOptions);

    [Fact]
    public async Task Handle_NewInvitee_PersistsInvitationAndSendsEmail()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, 1);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", null)
            .WithActiveCount(workspaceId, 0)
            .CapturingUpsert();
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        var result = await handler.Handle(
            new InviteMemberCommand(workspaceId, inviterId, "  Invitee@Test.Local  ", null), CancellationToken.None);

        // Assert
        Assert.Equal("invitee@test.local", result.Email);
        Assert.Equal(invitationRepo.UpsertedInvitation?.Id, result.Id);
        emailSender.VerifySentOnce();
        dailyUsageRepo.VerifyIncrementCalledOnce();
    }

    [Fact]
    public async Task Handle_DefaultLanguage_SendsRussianEmailWithInviterNameAndExpiryDate()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, 1);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", null)
            .WithActiveCount(workspaceId, 0)
            .CapturingUpsert();
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        await handler.Handle(
            new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", null), CancellationToken.None);

        // Assert
        Assert.Contains("Inviter Name", emailSender.SentMessage?.HtmlBody);
        Assert.Contains("пригласил", emailSender.SentMessage?.HtmlBody);
        Assert.Contains("Приглашение действует до", emailSender.SentMessage?.HtmlBody);
    }

    [Fact]
    public async Task Handle_EnglishLanguage_SendsEnglishEmailWithInviterNameAndExpiryDate()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, 1);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", null)
            .WithActiveCount(workspaceId, 0)
            .CapturingUpsert();
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        await handler.Handle(
            new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", "en"), CancellationToken.None);

        // Assert
        Assert.Contains("Inviter Name invited you", emailSender.SentMessage?.HtmlBody);
        Assert.Contains("This invitation expires on", emailSender.SentMessage?.HtmlBody);
    }

    [Fact]
    public async Task Handle_WorkspaceNameWithHtmlChars_EncodesItInBodyButNotInSubject()
    {
        // Arrange — workspace/inviter names are user-controlled; the HTML body must escape them,
        // but the plain-text subject must keep the literal characters.
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "Tom & Jerry", CreatedAt = DateTimeOffset.UtcNow };
        var inviter = new User { Id = inviterId, Email = "inviter@test.local", Name = "A & B", CreatedAt = DateTimeOffset.UtcNow };
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, workspace)
            .WithActiveMemberCount(workspaceId, 1);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", null)
            .WithActiveCount(workspaceId, 0)
            .CapturingUpsert();
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, inviter);
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        await handler.Handle(
            new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", "en"), CancellationToken.None);

        // Assert
        Assert.Contains("Tom &amp; Jerry", emailSender.SentMessage?.HtmlBody);
        Assert.Contains("A &amp; B", emailSender.SentMessage?.HtmlBody);
        Assert.Contains("A & B", emailSender.SentMessage?.Subject);
        Assert.DoesNotContain("&amp;", emailSender.SentMessage?.Subject);
    }

    [Fact]
    public async Task Handle_EmailAlreadyMember_ThrowsAlreadyMember()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var existingUser = new User { Id = Guid.NewGuid(), Email = "member@test.local", CreatedAt = DateTimeOffset.UtcNow };
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithMembership(workspaceId, existingUser.Id, WorkspaceRole.Member);
        var userRepo = new UserRepositoryMock()
            .WithUser("member@test.local", existingUser)
            .WithUserById(inviterId, DefaultInviter(inviterId));
        var invitationRepo = new WorkspaceInvitationRepositoryMock();
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new InviteMemberCommand(workspaceId, inviterId, "member@test.local", null), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.AlreadyMember, exception.Code);
        emailSender.VerifyNeverSent();
        dailyUsageRepo.VerifyIncrementNotCalled();
    }

    [Fact]
    public async Task Handle_WorkspaceFull_ThrowsWorkspaceFull()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, WorkspaceLimits.MaxMembers);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", null)
            .WithActiveCount(workspaceId, 0);
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", null), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.WorkspaceFull, exception.Code);
        emailSender.VerifyNeverSent();
        dailyUsageRepo.VerifyIncrementNotCalled();
    }

    [Fact]
    public async Task Handle_ResendOfStillActiveInvitation_SkipsCapCheck()
    {
        // Arrange — workspace already at the member cap, but this email already has an active pending
        // invite, so resending it doesn't change occupancy and shouldn't be blocked by the cap.
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var existingInvitation = new WorkspaceInvitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = "invitee@test.local",
            InvitedByUserId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(3),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-4),
        };
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, WorkspaceLimits.MaxMembers);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", existingInvitation)
            .CapturingUpsert();
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        var result = await handler.Handle(
            new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", null), CancellationToken.None);

        // Assert
        Assert.Equal(existingInvitation.Id, result.Id);
        emailSender.VerifySentOnce();
        dailyUsageRepo.VerifyIncrementCalledOnce();
    }

    [Fact]
    public async Task Handle_ResendOfExpiredInvitation_StillAppliesCapCheck()
    {
        // Arrange — an expired row occupies no active slot, so reviving it via resend must go
        // through the same cap check as a brand-new invitation, not be silently exempted.
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var existingInvitation = new WorkspaceInvitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = "invitee@test.local",
            InvitedByUserId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
        };
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, WorkspaceLimits.MaxMembers);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", existingInvitation)
            .WithActiveCount(workspaceId, 0)
            .CapturingUpsert();
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(1);
        var emailSender = new EmailSenderMock();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", null), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.WorkspaceFull, exception.Code);
        emailSender.VerifyNeverSent();
        dailyUsageRepo.VerifyIncrementNotCalled();
    }

    [Fact]
    public async Task Handle_DailyQuotaExceeded_ThrowsDailyQuotaExceeded()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithWorkspace(workspaceId, DefaultWorkspace(workspaceId))
            .WithActiveMemberCount(workspaceId, 1);
        var invitationRepo = new WorkspaceInvitationRepositoryMock()
            .WithPendingInvitation(workspaceId, "invitee@test.local", null)
            .WithActiveCount(workspaceId, 0);
        var userRepo = new UserRepositoryMock().WithNoUser("invitee@test.local").WithUserById(inviterId, DefaultInviter(inviterId));
        var dailyUsageRepo = new WorkspaceDailyUsageRepositoryMock().WithCurrentCount(21);
        var emailSender = new EmailSenderMock();
        var handler = BuildHandler(workspaceRepo, invitationRepo, userRepo, dailyUsageRepo, emailSender);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new InviteMemberCommand(workspaceId, inviterId, "invitee@test.local", null), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.DailyQuotaExceeded, exception.Code);
        emailSender.VerifyNeverSent();
        dailyUsageRepo.VerifyIncrementNotCalled();
    }
}
