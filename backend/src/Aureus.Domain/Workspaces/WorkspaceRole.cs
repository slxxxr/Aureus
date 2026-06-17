namespace Aureus.Domain.Workspaces;

// Values encode privilege level: higher = more permissions. RequireWorkspaceRoleFilter relies on this ordering.
public enum WorkspaceRole
{
    Member = 1,
    Manager = 2,
    Owner = 3
}
