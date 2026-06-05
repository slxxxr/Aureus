using System.IdentityModel.Tokens.Jwt;
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
}
