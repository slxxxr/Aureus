using Aureus.Api.Contracts.Auth.Login;
using Aureus.Api.Contracts.Auth.Register;
using Aureus.UseCases.Auth.Login;
using Aureus.UseCases.Auth.Register.Complete;
using Aureus.UseCases.Auth.Register.Start;
using Aureus.UseCases.Auth.Register.Verify;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Auth;

[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(ISender sender) : ApiControllerBase
{
    [HttpPost("register/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartRegistrationAsync(
        StartRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new StartRegistrationCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("register/verify")]
    [ProducesResponseType(typeof(VerifyEmailCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmailCodeAsync(
        VerifyEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new VerifyEmailCodeCommand(request.Email, request.Code),
            cancellationToken);

        return Ok(new VerifyEmailCodeResponse(result.RegistrationToken));
    }

    [HttpPost("register/complete")]
    [ProducesResponseType(typeof(CompleteRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteRegistrationAsync(
        CompleteRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteRegistrationCommand(request.RegistrationToken, request.Password),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new CompleteRegistrationResponse(result.AccessToken));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(new LoginResponse(result.AccessToken));
    }
}
