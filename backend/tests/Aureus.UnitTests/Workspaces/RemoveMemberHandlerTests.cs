using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.RemoveMember;

namespace Aureus.UnitTests.Workspaces;

public sealed class RemoveMemberHandlerTests
{
    private static RemoveMemberHandler BuildHandler(WorkspaceRepositoryMock workspaceRepo) =>
        new(workspaceRepo.Object);

    [Fact]
    public async Task Handle_ManagerRemovesMember_SoftDeletesMember()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Member)
            .CapturingDeleteMember();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new RemoveMemberCommand(workspaceId, targetId, WorkspaceRole.Manager),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyDeleteMemberCalledOnce(workspaceId, targetId);
    }

    [Fact]
    public async Task Handle_OwnerRemovesManager_SoftDeletesMember()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Manager)
            .CapturingDeleteMember();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new RemoveMemberCommand(workspaceId, targetId, WorkspaceRole.Owner),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyDeleteMemberCalledOnce(workspaceId, targetId);
    }

    [Fact]
    public async Task Handle_TargetNotMember_ThrowsMemberNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithNoMembership(workspaceId, targetId);
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new RemoveMemberCommand(workspaceId, targetId, WorkspaceRole.Manager),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.MemberNotFound, exception.Code);
        workspaceRepo.VerifyDeleteMemberNotCalled();
    }

    [Fact]
    public async Task Handle_TargetIsOwner_ThrowsCannotRemoveOwner()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Owner);
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new RemoveMemberCommand(workspaceId, targetId, WorkspaceRole.Owner),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.CannotRemoveOwner, exception.Code);
        workspaceRepo.VerifyDeleteMemberNotCalled();
    }

    [Fact]
    public async Task Handle_ManagerRemovesManager_ThrowsInsufficientRole()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Manager);
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new RemoveMemberCommand(workspaceId, targetId, WorkspaceRole.Manager),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.InsufficientRole, exception.Code);
        workspaceRepo.VerifyDeleteMemberNotCalled();
    }
}
