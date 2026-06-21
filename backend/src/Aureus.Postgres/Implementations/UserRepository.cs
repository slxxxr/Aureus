using AutoMapper;
using Aureus.Domain.Users;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aureus.Postgres.Implementations;

public sealed class UserRepository(AureusDbContext dbContext, IMapper mapper) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userDb = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        return userDb is null ? null : mapper.Map<User>(userDb);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var userDb = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        return userDb is null ? null : mapper.Map<User>(userDb);
    }

    public async Task UpdateProfileAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users
            .Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Name, user.Name), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(mapper.Map<UserDb>(user));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.EmailAlreadyConfirmed, "Email is already registered.");
        }
    }
}
