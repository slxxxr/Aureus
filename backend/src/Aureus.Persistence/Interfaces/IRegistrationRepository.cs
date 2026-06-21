using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;

namespace Aureus.Persistence.Interfaces;

public interface IRegistrationRepository
{
    Task CreateUserWithWorkspaceAsync(User user, Workspace workspace, WorkspaceMember member, CancellationToken cancellationToken);
}
