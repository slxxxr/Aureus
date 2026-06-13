namespace Aureus.Infrastructure.Email.Interfaces;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
