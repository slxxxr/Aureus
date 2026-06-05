using System.IdentityModel.Tokens.Jwt;
using Aureus.UseCases.Common.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aureus.Api.Filters;

public sealed class ValidateWorkspaceMemberFilter(IWorkspaceRepository workspaceRepository) : IAsyncActionFilter
{
    private const string WorkspaceIdRouteKey = "workspaceId";

    internal static readonly object MembershipItemKey = new();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userIdValue = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!context.RouteData.Values.TryGetValue(WorkspaceIdRouteKey, out var routeValue) ||
            !Guid.TryParse(routeValue?.ToString(), out var workspaceId))
        {
            context.Result = new NotFoundResult();
            return;
        }

        var membership = await workspaceRepository.FindMembershipAsync(
            workspaceId,
            userId,
            context.HttpContext.RequestAborted);

        if (membership is null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        context.HttpContext.Items[MembershipItemKey] = membership;

        await next();
    }
}
