using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;

namespace Aureus.Persistence.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, Workspace workspace, WorkspaceMember workspaceMember, CancellationToken cancellationToken);

    Task UpdateProfileAsync(User user, CancellationToken cancellationToken);
}
