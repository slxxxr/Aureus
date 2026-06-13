using System.Security.Cryptography;
using System.Text;
using Aureus.Domain.Users;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Auth.Register.Verify;

public sealed class VerifyEmailCodeHandler(
    IEmailVerificationCodeRepository codeRepository,
    IRegistrationTokenService tokenService) : IRequestHandler<VerifyEmailCodeCommand, VerifyEmailCodeResult>
{
    private const string Purpose = nameof(EmailVerificationPurpose.Registration);

    public async Task<VerifyEmailCodeResult> Handle(VerifyEmailCodeCommand command, CancellationToken cancellationToken)
    {
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var inputCode = (command.Code ?? string.Empty).Trim();

        var record = await codeRepository.FindAsync(email, Purpose, cancellationToken);

        if (record is null)
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.CodeNotFound,
                "No verification code found. Please start registration again.");
        }

        if (record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await codeRepository.DeleteAsync(email, Purpose, cancellationToken);
            throw new EmailVerificationException(EmailVerificationErrorCode.CodeExpired,
                "The code has expired. Please request a new one.");
        }

        var inputHash = ComputeSha256(inputCode);

        if (inputHash != record.CodeHash)
        {
            if (record.AttemptsLeft <= 1)
            {
                await codeRepository.DeleteAsync(email, Purpose, cancellationToken);
                throw new EmailVerificationException(EmailVerificationErrorCode.TooManyAttempts,
                    "Too many incorrect attempts. Please request a new code.");
            }

            await codeRepository.DecrementAttemptsAsync(email, Purpose, cancellationToken);
            throw new EmailVerificationException(EmailVerificationErrorCode.InvalidCode, "Invalid code.");
        }

        await codeRepository.DeleteAsync(email, Purpose, cancellationToken);

        var registrationToken = tokenService.Generate(email, Purpose);
        return new VerifyEmailCodeResult(registrationToken);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
