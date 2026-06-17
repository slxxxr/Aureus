using Aureus.Api.Contracts.Workspaces;
using Aureus.UseCases.Workspaces.AcceptInvitation;
using Aureus.UseCases.Workspaces.DeclineInvitation;
using Aureus.UseCases.Workspaces.GetMyInvitations;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Workspaces;

[Route("api/users/me/invitations")]
public sealed class MyInvitationsController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyInvitationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var invitations = await sender.Send(new GetMyInvitationsQuery(CurrentUserId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<MyInvitationResponse>>(invitations));
    }

    [HttpPost("{invitationId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        await sender.Send(new AcceptInvitationCommand(invitationId, CurrentUserId), cancellationToken);

        return NoContent();
    }

    [HttpPost("{invitationId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeclineAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeclineInvitationCommand(invitationId, CurrentUserId), cancellationToken);

        return NoContent();
    }
}
