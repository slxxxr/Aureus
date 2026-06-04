using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Common.Persistence;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class UserRegistrationDbMock
{
    private readonly Mock<IUserRegistrationDb> _mock = new();

    public IUserRegistrationDb Object => _mock.Object;

    public User? SavedUser { get; private set; }

    public Workspace? SavedWorkspace { get; private set; }

    public WorkspaceMember? SavedWorkspaceMember { get; private set; }

    public UserRegistrationDbMock WithExistingEmail(string email)
    {
        _mock
            .Setup(db => db.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return this;
    }

    public UserRegistrationDbMock WithAvailableEmail(string email)
    {
        _mock
            .Setup(db => db.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return this;
    }

    public UserRegistrationDbMock CapturingRegistration()
    {
        _mock
            .Setup(db => db.AddAsync(
                It.IsAny<User>(),
                It.IsAny<Workspace>(),
                It.IsAny<WorkspaceMember>(),
                It.IsAny<CancellationToken>()))
            .Callback<User, Workspace, WorkspaceMember, CancellationToken>((user, workspace, workspaceMember, _) =>
            {
                SavedUser = user;
                SavedWorkspace = workspace;
                SavedWorkspaceMember = workspaceMember;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyRegistrationSavedOnce()
    {
        _mock.Verify(
            db => db.AddAsync(
                It.IsAny<User>(),
                It.IsAny<Workspace>(),
                It.IsAny<WorkspaceMember>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void VerifyRegistrationNotSaved()
    {
        _mock.Verify(
            db => db.AddAsync(
                It.IsAny<User>(),
                It.IsAny<Workspace>(),
                It.IsAny<WorkspaceMember>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
