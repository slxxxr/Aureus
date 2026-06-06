using Aureus.Domain.Workspaces;
using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
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
}
