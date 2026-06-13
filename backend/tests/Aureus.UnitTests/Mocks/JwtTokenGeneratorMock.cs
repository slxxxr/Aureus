using Aureus.Infrastructure.Security.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class JwtTokenGeneratorMock
{
    private readonly Mock<IJwtTokenGenerator> _mock = new();

    public IJwtTokenGenerator Object => _mock.Object;

    public JwtTokenGeneratorMock WithToken(Guid userId, string email, string token)
    {
        _mock
            .Setup(g => g.Generate(userId, email))
            .Returns(token);

        return this;
    }

    public JwtTokenGeneratorMock WithAnyToken(string token)
    {
        _mock
            .Setup(g => g.Generate(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(token);

        return this;
    }
}
