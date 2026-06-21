using Aureus.Domain.Users;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class UserRepositoryMock
{
    private readonly Mock<IUserRepository> _mock = new();

    public IUserRepository Object => _mock.Object;

    public User? SavedUser { get; private set; }

    public UserRepositoryMock WithExistingEmail(string email)
    {
        _mock
            .Setup(db => db.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return this;
    }

    public UserRepositoryMock WithAvailableEmail(string email)
    {
        _mock
            .Setup(db => db.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return this;
    }

    public UserRepositoryMock WithUser(string email, User user)
    {
        _mock
            .Setup(db => db.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        return this;
    }

    public UserRepositoryMock WithNoUser(string email)
    {
        _mock
            .Setup(db => db.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        return this;
    }

    public UserRepositoryMock WithUserById(Guid id, User user)
    {
        _mock
            .Setup(db => db.FindByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        return this;
    }

    public UserRepositoryMock CapturingAdd()
    {
        _mock
            .Setup(db => db.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => SavedUser = user)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyAddCalledOnce() =>
        _mock.Verify(db => db.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyAddNotCalled() =>
        _mock.Verify(db => db.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
}
