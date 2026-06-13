using System.Security.Cryptography;
using System.Text;
using Aureus.Domain.Users;
using Aureus.Persistence.Entities;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Register.Verify;

namespace Aureus.UnitTests.Auth;

public sealed class VerifyEmailCodeHandlerTests
{
    private const string Purpose = "Registration";

    private static string Sha256(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();

    private static EmailVerificationCodeDb MakeRecord(string email, string code, int attemptsLeft = 10,
        DateTimeOffset? expiresAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            Purpose = Purpose,
            CodeHash = Sha256(code),
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
            AttemptsLeft = attemptsLeft,
            SentAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task Handle_ValidCode_ReturnsRegistrationToken()
    {
        // Arrange
        const string email = "user@example.com";
        const string code = "123456";
        const string token = "reg.token.value";
        var record = MakeRecord(email, code);

        var codeRepository = new EmailVerificationCodeRepositoryMock().WithPendingCode(email, Purpose, record);
        var tokenService = new RegistrationTokenServiceMock().WithGeneratedToken(email, Purpose, token);
        var handler = new VerifyEmailCodeHandler(codeRepository.Object, tokenService.Object);

        // Act
        var result = await handler.Handle(new VerifyEmailCodeCommand(email, code), CancellationToken.None);

        // Assert
        Assert.Equal(token, result.RegistrationToken);
        codeRepository.VerifyDeletedOnce(email, Purpose);
    }

    [Fact]
    public async Task Handle_CodeNotFound_ThrowsEmailVerificationException()
    {
        // Arrange
        const string email = "user@example.com";

        var codeRepository = new EmailVerificationCodeRepositoryMock().WithNoPendingCode(email, Purpose);
        var tokenService = new RegistrationTokenServiceMock();
        var handler = new VerifyEmailCodeHandler(codeRepository.Object, tokenService.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new VerifyEmailCodeCommand(email, "123456"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.CodeNotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_CodeExpired_ThrowsEmailVerificationExceptionAndDeletes()
    {
        // Arrange
        const string email = "user@example.com";
        const string code = "123456";
        var record = MakeRecord(email, code, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var codeRepository = new EmailVerificationCodeRepositoryMock().WithPendingCode(email, Purpose, record);
        var tokenService = new RegistrationTokenServiceMock();
        var handler = new VerifyEmailCodeHandler(codeRepository.Object, tokenService.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new VerifyEmailCodeCommand(email, code), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.CodeExpired, exception.Code);
        codeRepository.VerifyDeletedOnce(email, Purpose);
    }

    [Fact]
    public async Task Handle_InvalidCode_ThrowsEmailVerificationExceptionAndDecrements()
    {
        // Arrange
        const string email = "user@example.com";
        var record = MakeRecord(email, "correct", attemptsLeft: 5);

        var codeRepository = new EmailVerificationCodeRepositoryMock().WithPendingCode(email, Purpose, record);
        var tokenService = new RegistrationTokenServiceMock();
        var handler = new VerifyEmailCodeHandler(codeRepository.Object, tokenService.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new VerifyEmailCodeCommand(email, "wrong1"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.InvalidCode, exception.Code);
        codeRepository.VerifyDecrementedOnce(email, Purpose);
    }

    [Fact]
    public async Task Handle_LastAttemptInvalidCode_ThrowsTooManyAttemptsAndDeletes()
    {
        // Arrange
        const string email = "user@example.com";
        var record = MakeRecord(email, "correct", attemptsLeft: 1);

        var codeRepository = new EmailVerificationCodeRepositoryMock().WithPendingCode(email, Purpose, record);
        var tokenService = new RegistrationTokenServiceMock();
        var handler = new VerifyEmailCodeHandler(codeRepository.Object, tokenService.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new VerifyEmailCodeCommand(email, "wrong1"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.TooManyAttempts, exception.Code);
        codeRepository.VerifyDeletedOnce(email, Purpose);
    }

    [Fact]
    public async Task Handle_EmailNormalized_NormalizesEmail()
    {
        // Arrange
        const string normalizedEmail = "user@example.com";
        const string code = "123456";
        const string token = "reg.token.value";
        var record = MakeRecord(normalizedEmail, code);

        var codeRepository = new EmailVerificationCodeRepositoryMock()
            .WithPendingCode(normalizedEmail, Purpose, record);
        var tokenService = new RegistrationTokenServiceMock()
            .WithGeneratedToken(normalizedEmail, Purpose, token);
        var handler = new VerifyEmailCodeHandler(codeRepository.Object, tokenService.Object);

        // Act
        var result = await handler.Handle(
            new VerifyEmailCodeCommand(" User@Example.COM ", code), CancellationToken.None);

        // Assert
        Assert.Equal(token, result.RegistrationToken);
    }
}
