using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.UpdateWorkspace;

namespace Aureus.UnitTests.Workspaces;

public sealed class UpdateWorkspaceHandlerTests
{
    private static Workspace ExistingWorkspace(string name = "My Workspace") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

    [Fact]
    public async Task Handle_WorkspaceNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var repository = new WorkspaceRepositoryMock().WithNoWorkspace(workspaceId);
        var handler = new UpdateWorkspaceHandler(repository.Object);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceException>(() =>
            handler.Handle(new UpdateWorkspaceCommand(workspaceId, Name: "New Name"), CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_NameProvided_UpdatesName()
    {
        // Arrange
        var workspace = ExistingWorkspace(name: "Old Name");
        var repository = new WorkspaceRepositoryMock()
            .WithWorkspace(workspace.Id, workspace)
            .CapturingUpdate();
        var handler = new UpdateWorkspaceHandler(repository.Object);

        // Act
        var command = new UpdateWorkspaceCommand(workspace.Id, Name: "New Name");
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.Name, result.Name);
    }

    [Fact]
    public async Task Handle_NameProvided_NormalizesWhitespace()
    {
        // Arrange
        var workspace = ExistingWorkspace(name: "Old Name");
        var repository = new WorkspaceRepositoryMock()
            .WithWorkspace(workspace.Id, workspace)
            .CapturingUpdate();
        var handler = new UpdateWorkspaceHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateWorkspaceCommand(workspace.Id, Name: "  New Name  "),
            CancellationToken.None);

        // Assert
        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task Handle_NameNull_LeavesNameUnchanged()
    {
        // Arrange
        var workspace = ExistingWorkspace(name: "Original");
        var repository = new WorkspaceRepositoryMock()
            .WithWorkspace(workspace.Id, workspace)
            .CapturingUpdate();
        var handler = new UpdateWorkspaceHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateWorkspaceCommand(workspace.Id, Name: null),
            CancellationToken.None);

        // Assert
        Assert.Equal(workspace.Name, result.Name);
    }
}
