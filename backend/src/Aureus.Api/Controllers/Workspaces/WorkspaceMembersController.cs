using Aureus.Api.Filters;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Workspaces.LeaveWorkspace;
using Aureus.UseCases.Workspaces.RemoveMember;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Workspaces;

[Route("api/workspaces/{workspaceId:guid}")]
public sealed class WorkspaceMembersController(ISender sender) : ApiControllerBase
{
    [HttpDelete("members/{userId:guid}")]
    [RequireWorkspaceRole(WorkspaceRole.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RemoveMemberCommand(workspaceId, userId, CurrentWorkspaceMembership.Role),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("leave")]
    [RequireWorkspaceRole(WorkspaceRole.Member)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new LeaveWorkspaceCommand(workspaceId, CurrentUserId, CurrentWorkspaceMembership.Role),
            cancellationToken);

        return NoContent();
    }
}
