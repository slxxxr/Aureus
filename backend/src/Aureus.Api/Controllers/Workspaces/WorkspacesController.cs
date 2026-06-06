using Aureus.Api.Contracts.Workspaces;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Workspaces.CreateWorkspace;
using Aureus.UseCases.Workspaces.DeleteWorkspace;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
using Aureus.UseCases.Workspaces.UpdateWorkspace;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Workspaces;

[Route("api/workspaces")]
public sealed class WorkspacesController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserWorkspacesAsync(CancellationToken cancellationToken)
    {
        var workspaces = await sender.Send(new GetUserWorkspacesQuery(CurrentUserId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<WorkspaceResponse>>(workspaces));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await sender.Send(new CreateWorkspaceCommand(CurrentUserId, request.Name), cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new WorkspaceResponse(workspace.Id, workspace.Name, nameof(WorkspaceRole.Owner)));
    }

    [HttpPatch("{workspaceId:guid}")]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        Guid workspaceId,
        [FromBody] UpdateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var workspace = await sender.Send(new UpdateWorkspaceCommand(workspaceId, CurrentUserId, request.Name), cancellationToken);

        return Ok(new WorkspaceResponse(workspace.Id, workspace.Name, nameof(WorkspaceRole.Owner)));
    }

    [HttpDelete("{workspaceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWorkspaceCommand(workspaceId, CurrentUserId), cancellationToken);

        return NoContent();
    }
}
