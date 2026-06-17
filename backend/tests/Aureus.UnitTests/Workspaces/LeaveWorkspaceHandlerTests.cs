using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.LeaveWorkspace;

namespace Aureus.UnitTests.Workspaces;

public sealed class LeaveWorkspaceHandlerTests
{
    private static LeaveWorkspaceHandler BuildHandler(WorkspaceRepositoryMock workspaceRepo) =>
        new(workspaceRepo.Object);

    [Fact]
    public async Task Handle_MemberLeaves_SoftDeletesMembership()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock().CapturingDeleteMember();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new LeaveWorkspaceCommand(workspaceId, userId, WorkspaceRole.Member),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyDeleteMemberCalledOnce(workspaceId, userId);
    }

    [Fact]
    public async Task Handle_ManagerLeaves_SoftDeletesMembership()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock().CapturingDeleteMember();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new LeaveWorkspaceCommand(workspaceId, userId, WorkspaceRole.Manager),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyDeleteMemberCalledOnce(workspaceId, userId);
    }

    [Fact]
    public async Task Handle_OwnerLeaves_ThrowsCannotLeaveAsOwner()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock();
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new LeaveWorkspaceCommand(workspaceId, userId, WorkspaceRole.Owner),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.CannotLeaveAsOwner, exception.Code);
        workspaceRepo.VerifyDeleteMemberNotCalled();
    }
}
