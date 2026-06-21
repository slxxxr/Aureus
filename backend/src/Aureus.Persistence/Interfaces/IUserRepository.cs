using Aureus.Domain.Users;

namespace Aureus.Persistence.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateProfileAsync(User user, CancellationToken cancellationToken);
}
