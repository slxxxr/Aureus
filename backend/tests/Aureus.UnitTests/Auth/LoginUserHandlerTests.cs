using Aureus.Domain.Users;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Login;

namespace Aureus.UnitTests.Auth;

public sealed class LoginUserHandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com", PasswordHash = "hashed:password123" };

        var userRepository = new UserRepositoryMock().WithUser("user@example.com", user);
        var passwordHasher = new PasswordHasherMock().WithVerify("password123", "hashed:password123", true);
        var jwtGenerator = new JwtTokenGeneratorMock().WithToken(userId, "user@example.com", "jwt.token.value");
        var handler = new LoginUserHandler(userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var result = await handler.Handle(
            new LoginUserCommand("user@example.com", "password123"),
            CancellationToken.None);

        // Assert
        Assert.Equal("jwt.token.value", result.AccessToken);
    }

    [Fact]
    public async Task Handle_EmailNotFound_ThrowsLoginException()
    {
        // Arrange
        var userRepository = new UserRepositoryMock().WithNoUser("unknown@example.com");
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = new LoginUserHandler(userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var exception = await Assert.ThrowsAsync<LoginException>(() =>
            handler.Handle(new LoginUserCommand("unknown@example.com", "password123"), CancellationToken.None));

        // Assert
        Assert.Equal(LoginErrorCode.InvalidCredentials, exception.Code);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsLoginException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com", PasswordHash = "hashed:password123" };

        var userRepository = new UserRepositoryMock().WithUser("user@example.com", user);
        var passwordHasher = new PasswordHasherMock().WithVerify("wrongpassword", "hashed:password123", false);
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = new LoginUserHandler(userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var exception = await Assert.ThrowsAsync<LoginException>(() =>
            handler.Handle(new LoginUserCommand("user@example.com", "wrongpassword"), CancellationToken.None));

        // Assert
        Assert.Equal(LoginErrorCode.InvalidCredentials, exception.Code);
    }

    [Fact]
    public async Task Handle_EmailNormalized_FindsByNormalizedEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@example.com", PasswordHash = "hashed:password123" };

        var userRepository = new UserRepositoryMock().WithUser("user@example.com", user);
        var passwordHasher = new PasswordHasherMock().WithVerify("password123", "hashed:password123", true);
        var jwtGenerator = new JwtTokenGeneratorMock().WithToken(userId, "user@example.com", "jwt.token.value");
        var handler = new LoginUserHandler(userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var result = await handler.Handle(
            new LoginUserCommand(" User@Example.COM ", "password123"),
            CancellationToken.None);

        // Assert
        Assert.Equal("jwt.token.value", result.AccessToken);
    }
}
