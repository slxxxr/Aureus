using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class WorkspaceRepositoryMock
{
    private readonly Mock<IWorkspaceRepository> _mock = new();

    public IWorkspaceRepository Object => _mock.Object;

    public Workspace? SavedWorkspace { get; private set; }

    public WorkspaceMember? SavedMember { get; private set; }

    public Workspace? UpdatedWorkspace { get; private set; }

    public WorkspaceRepositoryMock WithWorkspace(Guid id, Workspace workspace)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return this;
    }

    public WorkspaceRepositoryMock WithNoWorkspace(Guid id)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        return this;
    }

    public WorkspaceRepositoryMock CapturingAdd()
    {
        _mock
            .Setup(r => r.AddAsync(It.IsAny<Workspace>(), It.IsAny<WorkspaceMember>(), It.IsAny<CancellationToken>()))
            .Callback<Workspace, WorkspaceMember, CancellationToken>((workspace, member, _) =>
            {
                SavedWorkspace = workspace;
                SavedMember = member;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public WorkspaceRepositoryMock CapturingUpdate()
    {
        _mock
            .Setup(r => r.UpdateAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .Callback<Workspace, CancellationToken>((workspace, _) => UpdatedWorkspace = workspace)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyUpdateNotCalled() =>
        _mock.Verify(r => r.UpdateAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()), Times.Never);

    public WorkspaceRepositoryMock CapturingDelete()
    {
        _mock
            .Setup(r => r.DeleteAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyDeleteCalledOnce() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteNotCalled() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()), Times.Never);

    public WorkspaceRepositoryMock WithActiveMemberCount(Guid workspaceId, int count)
    {
        _mock
            .Setup(r => r.CountActiveMembersAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        return this;
    }

    public WorkspaceRepositoryMock WithMembership(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        _mock
            .Setup(r => r.FindMembershipAsync(workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMembership(workspaceId, userId, role));

        return this;
    }

    public WorkspaceRepositoryMock WithNoMembership(Guid workspaceId, Guid userId)
    {
        _mock
            .Setup(r => r.FindMembershipAsync(workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceMembership?)null);

        return this;
    }

    public WorkspaceRepositoryMock CapturingUpdateMemberRole()
    {
        _mock
            .Setup(r => r.UpdateMemberRoleAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyUpdateMemberRoleCalledOnce(Guid workspaceId, Guid userId, WorkspaceRole role) =>
        _mock.Verify(r => r.UpdateMemberRoleAsync(workspaceId, userId, role, It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyUpdateMemberRoleNotCalled() =>
        _mock.Verify(r => r.UpdateMemberRoleAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<CancellationToken>()), Times.Never);

    public WorkspaceRepositoryMock CapturingTransferOwnership()
    {
        _mock
            .Setup(r => r.TransferOwnershipAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyTransferOwnershipCalledOnce(Guid workspaceId, Guid fromUserId, Guid toUserId) =>
        _mock.Verify(r => r.TransferOwnershipAsync(workspaceId, fromUserId, toUserId, It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyTransferOwnershipNotCalled() =>
        _mock.Verify(r => r.TransferOwnershipAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

    public WorkspaceRepositoryMock CapturingDeleteMember()
    {
        _mock
            .Setup(r => r.DeleteMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyDeleteMemberCalledOnce(Guid workspaceId, Guid userId) =>
        _mock.Verify(r => r.DeleteMemberAsync(workspaceId, userId, It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteMemberNotCalled() =>
        _mock.Verify(r => r.DeleteMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
}
