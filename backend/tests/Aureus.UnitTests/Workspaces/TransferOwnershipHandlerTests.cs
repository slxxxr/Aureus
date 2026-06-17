using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.TransferOwnership;

namespace Aureus.UnitTests.Workspaces;

public sealed class TransferOwnershipHandlerTests
{
    private static TransferOwnershipHandler BuildHandler(WorkspaceRepositoryMock workspaceRepo) =>
        new(workspaceRepo.Object);

    [Fact]
    public async Task Handle_ValidTarget_TransfersOwnership()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Member)
            .CapturingTransferOwnership();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new TransferOwnershipCommand(workspaceId, ownerId, targetId),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyTransferOwnershipCalledOnce(workspaceId, ownerId, targetId);
    }

    [Fact]
    public async Task Handle_TargetIsSelf_ThrowsCannotTargetSelf()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock();
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new TransferOwnershipCommand(workspaceId, userId, userId),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.CannotTargetSelf, exception.Code);
        workspaceRepo.VerifyTransferOwnershipNotCalled();
    }

    [Fact]
    public async Task Handle_TargetNotMember_ThrowsMemberNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithNoMembership(workspaceId, targetId);
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new TransferOwnershipCommand(workspaceId, ownerId, targetId),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.MemberNotFound, exception.Code);
        workspaceRepo.VerifyTransferOwnershipNotCalled();
    }
}
