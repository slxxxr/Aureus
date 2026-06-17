using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.RevokeInvitation;

namespace Aureus.UnitTests.Workspaces;

public sealed class RevokeInvitationHandlerTests
{
    private static WorkspaceInvitation DefaultInvitation(Guid id, Guid workspaceId) => new()
    {
        Id = id,
        WorkspaceId = workspaceId,
        Email = "invitee@test.local",
        InvitedByUserId = Guid.NewGuid(),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_InvitationNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var repo = new WorkspaceInvitationRepositoryMock().WithNoInvitation(invitationId);
        var handler = new RevokeInvitationHandler(repo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new RevokeInvitationCommand(workspaceId, invitationId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_InvitationBelongsToDifferentWorkspace_ThrowsNotFound()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, Guid.NewGuid());
        var repo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingDelete();
        var handler = new RevokeInvitationHandler(repo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceInvitationException>(() =>
            handler.Handle(new RevokeInvitationCommand(Guid.NewGuid(), invitationId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceInvitationErrorCode.NotFound, exception.Code);
        repo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesInvitation()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitation = DefaultInvitation(invitationId, workspaceId);
        var repo = new WorkspaceInvitationRepositoryMock().WithInvitation(invitationId, invitation).CapturingDelete();
        var handler = new RevokeInvitationHandler(repo.Object);

        // Act
        await handler.Handle(new RevokeInvitationCommand(workspaceId, invitationId), CancellationToken.None);

        // Assert
        Assert.Equal(invitationId, repo.DeletedInvitationId);
    }
}
