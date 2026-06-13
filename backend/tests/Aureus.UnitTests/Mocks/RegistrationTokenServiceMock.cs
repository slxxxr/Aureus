using Aureus.Infrastructure.Email;
using Aureus.Infrastructure.Email.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class RegistrationTokenServiceMock
{
    private readonly Mock<IRegistrationTokenService> _mock = new();

    public IRegistrationTokenService Object => _mock.Object;

    public RegistrationTokenServiceMock WithGeneratedToken(string email, string purpose, string token)
    {
        _mock
            .Setup(s => s.Generate(email, purpose))
            .Returns(token);

        return this;
    }

    public RegistrationTokenServiceMock WithValidToken(string token, string email, string purpose)
    {
        _mock
            .Setup(s => s.TryValidate(token))
            .Returns(new RegistrationTokenPayload(email, purpose));

        return this;
    }

    public RegistrationTokenServiceMock WithInvalidToken(string token)
    {
        _mock
            .Setup(s => s.TryValidate(token))
            .Returns((RegistrationTokenPayload?)null);

        return this;
    }
}
