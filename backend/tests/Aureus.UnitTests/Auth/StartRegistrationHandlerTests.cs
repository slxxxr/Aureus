using Aureus.Domain.Users;
using Aureus.Persistence.Entities;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Auth.Register.Start;

namespace Aureus.UnitTests.Auth;

public sealed class StartRegistrationHandlerTests
{
    private const string Purpose = "Registration";

    [Fact]
    public async Task Handle_ValidEmail_UpsertCodeAndSendsEmail()
    {
        // Arrange
        const string email = "user@example.com";

        var userRepository = new UserRepositoryMock().WithAvailableEmail(email);
        var codeRepository = new EmailVerificationCodeRepositoryMock()
            .WithNoPendingCode(email, Purpose)
            .CapturingUpsert();
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = new StartRegistrationHandler(userRepository.Object, codeRepository.Object, emailSender.Object);

        // Act
        await handler.Handle(new StartRegistrationCommand(email), CancellationToken.None);

        // Assert
        codeRepository.VerifyUpsertedOnce();
        emailSender.VerifySentOnce();
        Assert.Equal(email, emailSender.SentMessage?.To);
        Assert.NotNull(codeRepository.UpsertedRecord?.CodeHash);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsEmailVerificationException()
    {
        // Arrange
        var userRepository = new UserRepositoryMock();
        var codeRepository = new EmailVerificationCodeRepositoryMock();
        var emailSender = new EmailSenderMock();
        var handler = new StartRegistrationHandler(userRepository.Object, codeRepository.Object, emailSender.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new StartRegistrationCommand("not-an-email"), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.InvalidEmail, exception.Code);
        emailSender.VerifyNeverSent();
    }

    [Fact]
    public async Task Handle_EmailAlreadyConfirmed_ThrowsEmailVerificationException()
    {
        // Arrange
        const string email = "confirmed@example.com";

        var userRepository = new UserRepositoryMock().WithExistingEmail(email);
        var codeRepository = new EmailVerificationCodeRepositoryMock();
        var emailSender = new EmailSenderMock();
        var handler = new StartRegistrationHandler(userRepository.Object, codeRepository.Object, emailSender.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new StartRegistrationCommand(email), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.EmailAlreadyConfirmed, exception.Code);
        emailSender.VerifyNeverSent();
    }

    [Fact]
    public async Task Handle_RateLimited_ThrowsEmailVerificationException()
    {
        // Arrange
        const string email = "user@example.com";
        var recentCode = new EmailVerificationCodeDb
        {
            Id = Guid.NewGuid(),
            Email = email,
            Purpose = Purpose,
            CodeHash = "hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            AttemptsLeft = 10,
            SentAt = DateTimeOffset.UtcNow.AddSeconds(-30),
            CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-30),
        };

        var userRepository = new UserRepositoryMock().WithAvailableEmail(email);
        var codeRepository = new EmailVerificationCodeRepositoryMock().WithPendingCode(email, Purpose, recentCode);
        var emailSender = new EmailSenderMock();
        var handler = new StartRegistrationHandler(userRepository.Object, codeRepository.Object, emailSender.Object);

        // Act
        var exception = await Assert.ThrowsAsync<EmailVerificationException>(() =>
            handler.Handle(new StartRegistrationCommand(email), CancellationToken.None));

        // Assert
        Assert.Equal(EmailVerificationErrorCode.RateLimited, exception.Code);
        emailSender.VerifyNeverSent();
    }

    [Fact]
    public async Task Handle_EmailNormalized_NormalizesEmail()
    {
        // Arrange
        const string normalizedEmail = "user@example.com";

        var userRepository = new UserRepositoryMock().WithAvailableEmail(normalizedEmail);
        var codeRepository = new EmailVerificationCodeRepositoryMock()
            .WithNoPendingCode(normalizedEmail, Purpose)
            .CapturingUpsert();
        var emailSender = new EmailSenderMock().CapturingSent();
        var handler = new StartRegistrationHandler(userRepository.Object, codeRepository.Object, emailSender.Object);

        // Act
        await handler.Handle(new StartRegistrationCommand(" User@Example.COM "), CancellationToken.None);

        // Assert
        Assert.Equal(normalizedEmail, emailSender.SentMessage?.To);
        Assert.Equal(normalizedEmail, codeRepository.UpsertedRecord?.Email);
    }
}
