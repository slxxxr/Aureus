using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.DeleteWorkspace;

namespace Aureus.UnitTests.Workspaces;

public sealed class DeleteWorkspaceHandlerTests
{
    private static Workspace DefaultWorkspace(Guid? id = null, Guid? ownerId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        OwnerUserId = ownerId ?? Guid.NewGuid(),
        Name = "Personal",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_WorkspaceNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var repo = new WorkspaceRepositoryMock().WithNoWorkspace(workspaceId);
        var handler = new DeleteWorkspaceHandler(repo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceException>(() =>
            handler.Handle(new DeleteWorkspaceCommand(workspaceId, Guid.NewGuid()), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceErrorCode.NotFound, exception.Code);
        repo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var workspace = DefaultWorkspace(ownerId: ownerId);
        var repo = new WorkspaceRepositoryMock().WithWorkspace(workspace.Id, workspace).CapturingDelete();
        var handler = new DeleteWorkspaceHandler(repo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceException>(() =>
            handler.Handle(new DeleteWorkspaceCommand(workspace.Id, requestingUserId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceErrorCode.Forbidden, exception.Code);
        repo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesWorkspace()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = DefaultWorkspace(ownerId: ownerId);
        var repo = new WorkspaceRepositoryMock().WithWorkspace(workspace.Id, workspace).CapturingDelete();
        var handler = new DeleteWorkspaceHandler(repo.Object);

        // Act
        await handler.Handle(new DeleteWorkspaceCommand(workspace.Id, ownerId), CancellationToken.None);

        // Assert
        repo.VerifyDeleteCalledOnce();
    }
}
