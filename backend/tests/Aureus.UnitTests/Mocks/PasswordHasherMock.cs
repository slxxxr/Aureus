using Aureus.Infrastructure.Security.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class PasswordHasherMock
{
    private readonly Mock<IPasswordHasher> _mock = new();

    public IPasswordHasher Object => _mock.Object;

    public PasswordHasherMock WithHash(string password, string passwordHash)
    {
        _mock
            .Setup(hasher => hasher.Hash(password))
            .Returns(passwordHash);

        return this;
    }

    public PasswordHasherMock WithVerify(string password, string passwordHash, bool result)
    {
        _mock
            .Setup(hasher => hasher.Verify(password, passwordHash))
            .Returns(result);

        return this;
    }
}
