using Aureus.Api.Contracts.Profile;
using Aureus.UseCases.Profile.GetProfile;
using Aureus.UseCases.Profile.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Profile;

[Route("api/users/me")]
public sealed class ProfileController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProfileQuery(CurrentUserId), cancellationToken);
        return Ok(new UserProfileResponse(result.Id, result.Email, result.Name));
    }

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateProfileCommand(CurrentUserId, request.Name), cancellationToken);
        return NoContent();
    }
}
