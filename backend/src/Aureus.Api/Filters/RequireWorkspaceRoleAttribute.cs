using Aureus.Domain.Workspaces;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireWorkspaceRoleAttribute : TypeFilterAttribute
{
    public RequireWorkspaceRoleAttribute(WorkspaceRole minimumRole)
        : base(typeof(RequireWorkspaceRoleFilter))
    {
        Arguments = [minimumRole];
    }
}
