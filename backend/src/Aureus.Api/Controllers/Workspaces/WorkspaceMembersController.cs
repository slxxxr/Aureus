using Aureus.Api.Contracts.Workspaces;
using Aureus.Api.Filters;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Workspaces.GetWorkspaceMembers;
using Aureus.UseCases.Workspaces.LeaveWorkspace;
using Aureus.UseCases.Workspaces.RemoveMember;
using Aureus.UseCases.Workspaces.TransferOwnership;
using Aureus.UseCases.Workspaces.UpdateMemberRole;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Workspaces;

[Route("api/workspaces/{workspaceId:guid}")]
public sealed class WorkspaceMembersController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet("members")]
    [RequireWorkspaceRole(WorkspaceRole.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceMemberResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembersAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var members = await sender.Send(new GetWorkspaceMembersQuery(workspaceId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<WorkspaceMemberResponse>>(members));
    }

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

    [HttpPatch("members/{userId:guid}/role")]
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMemberRoleAsync(
        Guid workspaceId,
        Guid userId,
        [FromBody] UpdateMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateMemberRoleCommand(workspaceId, CurrentUserId, userId, request.Role),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("transfer-ownership")]
    [RequireWorkspaceRole(WorkspaceRole.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransferOwnershipAsync(
        Guid workspaceId,
        [FromBody] TransferOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new TransferOwnershipCommand(workspaceId, CurrentUserId, request.UserId),
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
