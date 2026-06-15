using Aureus.Domain.Workspaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aureus.Api.Filters;

public sealed class RequireWorkspaceRoleFilter(WorkspaceRole minimumRole) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var membership = context.HttpContext.Items[ValidateWorkspaceMemberFilter.MembershipItemKey]
            as WorkspaceMembership;

        if (membership is null || membership.Role < minimumRole)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
