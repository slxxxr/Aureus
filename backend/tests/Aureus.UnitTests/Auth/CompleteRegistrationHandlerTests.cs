using Aureus.Domain.Users;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Register.Complete;

namespace Aureus.UnitTests.Auth;

public sealed class CompleteRegistrationHandlerTests
{
    private const string Purpose = "Registration";

    [Fact]
    public async Task Handle_ValidToken_RegistersUserAndReturnsAccessToken()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "reg.token.value";
        const string accessToken = "jwt.access.token";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var userRepository = new UserRepositoryMock().CapturingRegistration();
        var passwordHasher = new PasswordHasherMock().WithHash("securepass", "hashed:securepass");
        var jwtGenerator = new JwtTokenGeneratorMock().WithAnyToken(accessToken);
        var handler = new CompleteRegistrationHandler(
            tokenService.Object, userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var result = await handler.Handle(
            new CompleteRegistrationCommand(token, "securepass"), CancellationToken.None);

        // Assert
        Assert.Equal(accessToken, result.AccessToken);
        userRepository.VerifyRegistrationSavedOnce();
        Assert.Equal(email, userRepository.SavedUser?.Email);
        Assert.Equal("hashed:securepass", userRepository.SavedUser?.PasswordHash);
        Assert.Equal("Personal", userRepository.SavedWorkspace?.Name);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsEmailVerificationException()
    {
        // Arrange
        const string token = "bad.token";

        var tokenService = new RegistrationTokenServiceMock().WithInvalidToken(token);
        var userRepository = new UserRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = new CompleteRegistrationHandler(
            tokenService.Object, userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new CompleteRegistrationCommand(token, "securepass"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.RegistrationTokenInvalid, exception.Code);
        userRepository.VerifyRegistrationNotSaved();
    }

    [Fact]
    public async Task Handle_PasswordTooShort_ThrowsEmailVerificationException()
    {
        // Arrange
        const string token = "reg.token.value";
        const string email = "user@example.com";

        var tokenService = new RegistrationTokenServiceMock().WithValidToken(token, email, Purpose);
        var userRepository = new UserRepositoryMock();
        var passwordHasher = new PasswordHasherMock();
        var jwtGenerator = new JwtTokenGeneratorMock();
        var handler = new CompleteRegistrationHandler(
            tokenService.Object, userRepository.Object, passwordHasher.Object, jwtGenerator.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new CompleteRegistrationCommand(token, "short"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.InvalidPassword, exception.Code);
        userRepository.VerifyRegistrationNotSaved();
    }
}
