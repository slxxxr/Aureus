using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.CreateWorkspace;

namespace Aureus.UnitTests.Workspaces;

public sealed class CreateWorkspaceHandlerTests
{
    private static CreateWorkspaceCommand DefaultCommand(
        string name = "My Workspace",
        Guid? userId = null) =>
        new(UserId: userId ?? Guid.NewGuid(), Name: name);

    [Fact]
    public async Task Handle_ValidCommand_CreatesWorkspace()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var repository = new WorkspaceRepositoryMock().CapturingAdd();
        var handler = new CreateWorkspaceHandler(repository.Object);

        // Act
        var result = await handler.Handle(DefaultCommand(name: "My Workspace", userId: userId), CancellationToken.None);

        // Assert
        Assert.Equal("My Workspace", result.Name);
        Assert.Equal(userId, result.OwnerUserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCreatorAsOwnerMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var repository = new WorkspaceRepositoryMock().CapturingAdd();
        var handler = new CreateWorkspaceHandler(repository.Object);

        // Act
        var result = await handler.Handle(DefaultCommand(userId: userId), CancellationToken.None);

        // Assert
        Assert.Equal(WorkspaceRole.Owner, repository.SavedMember!.Role);
        Assert.Equal(result.Id, repository.SavedMember!.WorkspaceId);
        Assert.Equal(userId, repository.SavedMember!.UserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespace()
    {
        // Arrange
        var repository = new WorkspaceRepositoryMock().CapturingAdd();
        var handler = new CreateWorkspaceHandler(repository.Object);

        // Act
        var result = await handler.Handle(DefaultCommand(name: "  My Workspace  "), CancellationToken.None);

        // Assert
        Assert.Equal("My Workspace", result.Name);
    }
}
