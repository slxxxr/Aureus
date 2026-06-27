using System.Security.Cryptography;
using System.Text;
using Aureus.Domain.Users;
using Aureus.Infrastructure.Email;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Auth.Register.Start;

public sealed class StartRegistrationHandler(
    IUserRepository userRepository,
    IEmailVerificationCodeRepository codeRepository,
    IEmailSender emailSender) : IRequestHandler<StartRegistrationCommand>
{
    private const string Purpose = nameof(EmailVerificationPurpose.Registration);
    private const int AttemptsAllowed = 10;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public async Task Handle(StartRegistrationCommand command, CancellationToken cancellationToken)
    {
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var language = NormalizeLanguage(command.Language);

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new EmailVerificationException(
                EmailVerificationErrorCode.EmailAlreadyConfirmed, "Email is already registered.");
        }

        var existing = await codeRepository.FindAsync(email, Purpose, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (existing is not null && now - existing.SentAt < ResendCooldown)
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.RateLimited,
                "Please wait before requesting another code.");
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var codeHash = ComputeSha256(code);

        await codeRepository.UpsertAsync(new EmailVerificationCodeDb
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            Email = email,
            Purpose = Purpose,
            CodeHash = codeHash,
            ExpiresAt = now.Add(CodeLifetime),
            AttemptsLeft = AttemptsAllowed,
            SentAt = now,
            CreatedAt = existing?.CreatedAt ?? now,
        }, cancellationToken);

        var (subject, htmlBody) = RegistrationEmailTemplates.Build(language, code);
        await emailSender.SendAsync(new EmailMessage(
            To: email,
            Subject: subject,
            HtmlBody: htmlBody), cancellationToken);
    }

    private static string NormalizeLanguage(string? language) =>
        (language ?? string.Empty).Trim().ToLowerInvariant() == "en" ? "en" : "ru";

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
