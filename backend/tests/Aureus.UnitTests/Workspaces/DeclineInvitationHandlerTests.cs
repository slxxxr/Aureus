using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.DeclineInvitation;

namespace Aureus.UnitTests.Workspaces;

public sealed class DeclineInvitationHandlerTests
{
    private static WorkspaceInvitation DefaultInvitation(Guid id, string email = "invitee@test.local") => new()
    {
        Id = id,
        WorkspaceId = Guid.NewGuid(),
        Email = email,
        InvitedByUserId = Guid.NewGuid(),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_InvitationNotFound_ThrowsNotFound()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithNoInvitation(invitationId);
        var userRepo = new UserRepositoryMock();
        var handler = new DeclineInvitationHandler(invitationRepo.Object, userRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new DeclineInvitationCommand(invitationId, Guid.NewGuid()), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_EmailDoesNotMatchUser_ThrowsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, email: "invitee@test.local");
        var user = new User { Id = userId, Email = "someone-else@test.local", CreatedAt = DateTimeOffset.UtcNow };

        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingDelete();
        var userRepo = new UserRepositoryMock().WithUserById(userId, user);
        var handler = new DeclineInvitationHandler(invitationRepo.Object, userRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new DeclineInvitationCommand(invitationId, userId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.Forbidden, exception.Code);
        invitationRepo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesInvitation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, email: "invitee@test.local");
        var user = new User { Id = userId, Email = "invitee@test.local", CreatedAt = DateTimeOffset.UtcNow };

        var invitationRepo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingDelete();
        var userRepo = new UserRepositoryMock().WithUserById(userId, user);
        var handler = new DeclineInvitationHandler(invitationRepo.Object, userRepo.Object);

        // Act
        await handler.Handle(new DeclineInvitationCommand(invitationId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(invitationId, invitationRepo.DeletedInvitationId);
    }
}
