using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class EmailVerificationCodeRepositoryMock
{
    private readonly Mock<IEmailVerificationCodeRepository> _mock = new();

    public IEmailVerificationCodeRepository Object => _mock.Object;

    public EmailVerificationCodeDb? UpsertedRecord { get; private set; }

    public EmailVerificationCodeRepositoryMock WithNoPendingCode(string email, string purpose)
    {
        _mock
            .Setup(r => r.FindAsync(email, purpose, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationCodeDb?)null);

        return this;
    }

    public EmailVerificationCodeRepositoryMock WithPendingCode(string email, string purpose, EmailVerificationCodeDb code)
    {
        _mock
            .Setup(r => r.FindAsync(email, purpose, It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);

        return this;
    }

    public EmailVerificationCodeRepositoryMock CapturingUpsert()
    {
        _mock
            .Setup(r => r.UpsertAsync(It.IsAny<EmailVerificationCodeDb>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationCodeDb, CancellationToken>((record, _) => UpsertedRecord = record)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyUpsertedOnce()
    {
        _mock.Verify(r => r.UpsertAsync(It.IsAny<EmailVerificationCodeDb>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public void VerifyDeletedOnce(string email, string purpose)
    {
        _mock.Verify(r => r.DeleteAsync(email, purpose, It.IsAny<CancellationToken>()), Times.Once);
    }

    public void VerifyDecrementedOnce(string email, string purpose)
    {
        _mock.Verify(r => r.DecrementAttemptsAsync(email, purpose, It.IsAny<CancellationToken>()), Times.Once);
    }
}
