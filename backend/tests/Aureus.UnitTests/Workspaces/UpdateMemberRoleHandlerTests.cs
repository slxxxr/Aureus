using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Workspaces.UpdateMemberRole;

namespace Aureus.UnitTests.Workspaces;

public sealed class UpdateMemberRoleHandlerTests
{
    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Manager)]
    public void Validator_AllowedRoles_PassesValidation(WorkspaceRole role)
    {
        // Arrange
        var validator = new UpdateMemberRoleCommandValidator();

        // Act
        var result = validator.Validate(
            new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role));

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_OwnerRole_FailsValidation()
    {
        // Arrange
        var validator = new UpdateMemberRoleCommandValidator();

        // Act
        var result = validator.Validate(
            new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner));

        // Assert
        Assert.False(result.IsValid);
    }


    private static UpdateMemberRoleHandler BuildHandler(WorkspaceRepositoryMock workspaceRepo) =>
        new(workspaceRepo.Object);

    [Fact]
    public async Task Handle_OwnerPromotesMember_UpdatesRole()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Member)
            .CapturingUpdateMemberRole();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new UpdateMemberRoleCommand(workspaceId, ownerId, targetId, WorkspaceRole.Manager),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyUpdateMemberRoleCalledOnce(workspaceId, targetId, WorkspaceRole.Manager);
    }

    [Fact]
    public async Task Handle_OwnerDemotesManager_UpdatesRole()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Manager)
            .CapturingUpdateMemberRole();
        var handler = BuildHandler(workspaceRepo);

        // Act
        await handler.Handle(
            new UpdateMemberRoleCommand(workspaceId, ownerId, targetId, WorkspaceRole.Member),
            CancellationToken.None);

        // Assert
        workspaceRepo.VerifyUpdateMemberRoleCalledOnce(workspaceId, targetId, WorkspaceRole.Member);
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
                new UpdateMemberRoleCommand(workspaceId, userId, userId, WorkspaceRole.Manager),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.CannotTargetSelf, exception.Code);
        workspaceRepo.VerifyUpdateMemberRoleNotCalled();
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
                new UpdateMemberRoleCommand(workspaceId, ownerId, targetId, WorkspaceRole.Manager),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.MemberNotFound, exception.Code);
        workspaceRepo.VerifyUpdateMemberRoleNotCalled();
    }

    [Fact]
    public async Task Handle_TargetIsOwner_ThrowsCannotChangeOwnerRole()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var workspaceRepo = new WorkspaceRepositoryMock()
            .WithMembership(workspaceId, targetId, WorkspaceRole.Owner);
        var handler = BuildHandler(workspaceRepo);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceMemberException>(() =>
            handler.Handle(
                new UpdateMemberRoleCommand(workspaceId, ownerId, targetId, WorkspaceRole.Member),
                CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceMemberErrorCode.CannotChangeOwnerRole, exception.Code);
        workspaceRepo.VerifyUpdateMemberRoleNotCalled();
    }
}
