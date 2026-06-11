using Aureus.Domain.Workspaces;
using System.IdentityModel.Tokens.Jwt;
using Aureus.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new InvalidOperationException("Authenticated request is missing a valid user ID claim.");
        }
    }

    protected WorkspaceMembership CurrentWorkspaceMembership =>
        HttpContext.Items[ValidateWorkspaceMemberFilter.MembershipItemKey] as WorkspaceMembership
        ?? throw new InvalidOperationException(
            $"{nameof(CurrentWorkspaceMembership)} is only available on endpoints decorated with [{nameof(ValidateWorkspaceMemberAttribute)}].");
}
