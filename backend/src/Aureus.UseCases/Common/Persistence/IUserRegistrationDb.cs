using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;

namespace Aureus.UseCases.Common.Persistence;

public interface IUserRegistrationDb
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, Workspace workspace, WorkspaceMember workspaceMember, CancellationToken cancellationToken);
}
