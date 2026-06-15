using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.DeleteWorkspace;

namespace Aureus.UnitTests.Workspaces;

public sealed class DeleteWorkspaceHandlerTests
{
    private static Workspace DefaultWorkspace(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        OwnerUserId = Guid.NewGuid(),
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
            handler.Handle(new DeleteWorkspaceCommand(workspaceId), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceErrorCode.NotFound, exception.Code);
        repo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesWorkspace()
    {
        // Arrange
        var workspace = DefaultWorkspace();
        var repo = new WorkspaceRepositoryMock().WithWorkspace(workspace.Id, workspace).CapturingDelete();
        var handler = new DeleteWorkspaceHandler(repo.Object);

        // Act
        await handler.Handle(new DeleteWorkspaceCommand(workspace.Id), CancellationToken.None);

        // Assert
        repo.VerifyDeleteCalledOnce();
    }
}
