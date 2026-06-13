using Aureus.Persistence.Entities;

namespace Aureus.Persistence.Interfaces;

public interface IEmailVerificationCodeRepository
{
    Task<EmailVerificationCodeDb?> FindAsync(string email, string purpose, CancellationToken cancellationToken);

    Task UpsertAsync(EmailVerificationCodeDb code, CancellationToken cancellationToken);

    Task DecrementAttemptsAsync(string email, string purpose, CancellationToken cancellationToken);

    Task DeleteAsync(string email, string purpose, CancellationToken cancellationToken);
}
