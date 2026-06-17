using Aureus.Api.Contracts.Workspaces;
using Aureus.Api.Filters;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Workspaces.GetWorkspaceInvitations;
using Aureus.UseCases.Workspaces.InviteMember;
using Aureus.UseCases.Workspaces.RevokeInvitation;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Workspaces;

[RequireWorkspaceRole(WorkspaceRole.Manager)]
[Route("api/workspaces/{workspaceId:guid}/invitations")]
public sealed class WorkspaceInvitationsController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var invitations = await sender.Send(new GetWorkspaceInvitationsQuery(workspaceId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<InvitationResponse>>(invitations));
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> InviteAsync(
        Guid workspaceId,
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InviteMemberCommand(workspaceId, CurrentUserId, request.Email, request.Language);
        var invitation = await sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, mapper.Map<InvitationResponse>(invitation));
    }

    [HttpDelete("{invitationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(
        Guid workspaceId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeInvitationCommand(workspaceId, invitationId), cancellationToken);

        return NoContent();
    }
}
