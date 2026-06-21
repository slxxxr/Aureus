using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class RegistrationRepositoryMock
{
    private readonly Mock<IRegistrationRepository> _mock = new();

    public IRegistrationRepository Object => _mock.Object;

    public User? SavedUser { get; private set; }

    public Workspace? SavedWorkspace { get; private set; }

    public WorkspaceMember? SavedMember { get; private set; }

    public RegistrationRepositoryMock CapturingCreate()
    {
        _mock
            .Setup(r => r.CreateUserWithWorkspaceAsync(
                It.IsAny<User>(), It.IsAny<Workspace>(), It.IsAny<WorkspaceMember>(), It.IsAny<CancellationToken>()))
            .Callback<User, Workspace, WorkspaceMember, CancellationToken>((user, workspace, member, _) =>
            {
                SavedUser = user;
                SavedWorkspace = workspace;
                SavedMember = member;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyCreateCalledOnce() =>
        _mock.Verify(r => r.CreateUserWithWorkspaceAsync(
            It.IsAny<User>(), It.IsAny<Workspace>(), It.IsAny<WorkspaceMember>(), It.IsAny<CancellationToken>()),
            Times.Once);

    public void VerifyCreateNotCalled() =>
        _mock.Verify(r => r.CreateUserWithWorkspaceAsync(
            It.IsAny<User>(), It.IsAny<Workspace>(), It.IsAny<WorkspaceMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
}
