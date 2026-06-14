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

        await emailSender.SendAsync(new EmailMessage(
            To: email,
            Subject: "Your Aureus verification code",
            HtmlBody: BuildEmailHtml(code)), cancellationToken);
    }

    private static string BuildEmailHtml(string code) => $"""
        <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px 24px">
          <h2 style="margin:0 0 8px">Confirm your email</h2>
          <p style="color:#6b7280;margin:0 0 24px">Enter this code in Aureus to complete registration.</p>
          <div style="font-size:36px;font-weight:700;letter-spacing:8px;text-align:center;
                      padding:20px;background:#f9fafb;border-radius:8px;margin-bottom:24px">
            {code}
          </div>
          <p style="color:#9ca3af;font-size:13px;margin:0">
            Code expires in 1 hour. If you did not request this, you can ignore this email.
          </p>
        </div>
        """;

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
