namespace Aureus.Infrastructure.Email;

public sealed record EmailMessage(string To, string Subject, string HtmlBody);
