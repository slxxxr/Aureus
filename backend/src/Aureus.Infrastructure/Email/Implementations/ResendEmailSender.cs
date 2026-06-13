using Aureus.Infrastructure.Email.Interfaces;
using Microsoft.Extensions.Options;
using ResendMessage = Resend.EmailMessage;

namespace Aureus.Infrastructure.Email.Implementations;

public sealed class ResendEmailSender(Resend.IResend resend, IOptions<ResendOptions> options)
    : IEmailSender
{
    private readonly ResendOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var resendMessage = new ResendMessage
        {
            From = _options.FromAddress,
            To = [message.To],
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
        };

        await resend.EmailSendAsync(resendMessage, cancellationToken);
    }
}
