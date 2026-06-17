using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.AcceptInvitation;

namespace Aureus.UnitTests.Workspaces;

public sealed class AcceptInvitationHandlerTests
{
    private static WorkspaceInvitation DefaultInvitation(
        Guid id, Guid workspaceId, string email = "invitee@test.local", DateTimeOffset? expiresAt = null) => new()
    {
        Id = id,
        WorkspaceId = workspaceId,
        Email = email,
        InvitedByUserId = Guid.NewGuid(),
        ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(7),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_InvitationNotFound_ThrowsNotFound()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithNoInvitation(invitationId);
        var userRepo = new UserRepositoryMock();
        var workspaceRepo = new WorkspaceRepositoryMock();
        var handler = new AcceptInvitationHandler(invitationRepo.Object, workspaceRepo.Object, userRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new AcceptInvitationCommand(invitationId, Guid.NewGuid()), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_InvitationExpired_ThrowsNotFound()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, Guid.NewGuid(), expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation);
        var userRepo = new UserRepositoryMock();
        var workspaceRepo = new WorkspaceRepositoryMock();
        var handler = new AcceptInvitationHandler(invitationRepo.Object, workspaceRepo.Object, userRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new AcceptInvitationCommand(invitationId, Guid.NewGuid()), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_EmailDoesNotMatchUser_ThrowsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, Guid.NewGuid(), email: "invitee@test.local");
        var user = new User { Id = userId, Email = "someone-else@test.local", CreatedAt = DateTimeOffset.UtcNow };

        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingAccept();
        var userRepo = new UserRepositoryMock().WithUserById(userId, user);
        var workspaceRepo = new WorkspaceRepositoryMock();
        var handler = new AcceptInvitationHandler(invitationRepo.Object, workspaceRepo.Object, userRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new AcceptInvitationCommand(invitationId, userId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.Forbidden, exception.Code);
        invitationRepo.VerifyAcceptNotCalled();
    }

    [Fact]
    public async Task Handle_WorkspaceFull_ThrowsWorkspaceFull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, workspaceId, email: "invitee@test.local");
        var user = new User { Id = userId, Email = "invitee@test.local", CreatedAt = DateTimeOffset.UtcNow };

        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingAccept();
        var userRepo = new UserRepositoryMock().WithUserById(userId, user);
        var workspaceRepo = new WorkspaceRepositoryMock().WithActiveMemberCount(workspaceId, WorkspaceLimits.MaxMembers);
        var handler = new AcceptInvitationHandler(invitationRepo.Object, workspaceRepo.Object, userRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new AcceptInvitationCommand(invitationId, userId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.WorkspaceFull, exception.Code);
        invitationRepo.VerifyAcceptNotCalled();
    }

    [Fact]
    public async Task Handle_ValidCommand_AcceptsInvitationAsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, workspaceId, email: "invitee@test.local");
        var user = new User { Id = userId, Email = "invitee@test.local", CreatedAt = DateTimeOffset.UtcNow };

        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingAccept();
        var userRepo = new UserRepositoryMock().WithUserById(userId, user);
        var workspaceRepo = new WorkspaceRepositoryMock().WithActiveMemberCount(workspaceId, 1);
        var handler = new AcceptInvitationHandler(invitationRepo.Object, workspaceRepo.Object, userRepo.Object);

        // Act
        await handler.Handle(new AcceptInvitationCommand(invitationId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(invitationId, invitationRepo.AcceptedInvitationId);
        Assert.Equal(WorkspaceRole.Member, invitationRepo.AcceptedMember?.Role);
        Assert.Equal(userId, invitationRepo.AcceptedMember?.UserId);
        Assert.Equal(workspaceId, invitationRepo.AcceptedMember?.WorkspaceId);
    }
}
