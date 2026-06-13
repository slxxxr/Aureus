using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

public sealed class EmailVerificationCodeRepository(AureusDbContext dbContext) : IEmailVerificationCodeRepository
{
    public Task<EmailVerificationCodeDb?> FindAsync(string email, string purpose, CancellationToken cancellationToken)
    {
        return dbContext.EmailVerificationCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email && x.Purpose == purpose, cancellationToken);
    }

    public async Task UpsertAsync(EmailVerificationCodeDb code, CancellationToken cancellationToken)
    {
        var existing = await dbContext.EmailVerificationCodes
            .FirstOrDefaultAsync(x => x.Email == code.Email && x.Purpose == code.Purpose, cancellationToken);

        if (existing is null)
        {
            dbContext.EmailVerificationCodes.Add(code);
        }
        else
        {
            existing.CodeHash = code.CodeHash;
            existing.ExpiresAt = code.ExpiresAt;
            existing.AttemptsLeft = code.AttemptsLeft;
            existing.SentAt = code.SentAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DecrementAttemptsAsync(string email, string purpose, CancellationToken cancellationToken)
    {
        return dbContext.EmailVerificationCodes
            .Where(x => x.Email == email && x.Purpose == purpose)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.AttemptsLeft, x => x.AttemptsLeft - 1),
                cancellationToken);
    }

    public Task DeleteAsync(string email, string purpose, CancellationToken cancellationToken)
    {
        return dbContext.EmailVerificationCodes
            .Where(x => x.Email == email && x.Purpose == purpose)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
