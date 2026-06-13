using Aureus.Infrastructure.Email;
using Aureus.Infrastructure.Email.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class EmailSenderMock
{
    private readonly Mock<IEmailSender> _mock = new();

    public IEmailSender Object => _mock.Object;

    public EmailMessage? SentMessage { get; private set; }

    public EmailSenderMock CapturingSent()
    {
        _mock
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => SentMessage = msg)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifySentOnce()
    {
        _mock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public void VerifyNeverSent()
    {
        _mock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
