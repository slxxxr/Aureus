using Aureus.Api.Contracts.Workspaces;
using Aureus.UseCases.Workspaces.CreateWorkspace;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
using Aureus.UseCases.Workspaces.UpdateWorkspace;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Workspaces;

[Route("api/workspaces")]
public sealed class WorkspacesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserWorkspacesAsync(CancellationToken cancellationToken)
    {
        var workspaces = await sender.Send(new GetUserWorkspacesQuery(CurrentUserId), cancellationToken);

        // TODO: map to response via AutoMapper (see Aureus.Api.Mappers.ContractMappings)
        var response = workspaces
            .Select(w => new WorkspaceResponse(w.Id, w.Name, w.Role))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await sender.Send(new CreateWorkspaceCommand(CurrentUserId, request.Name), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new WorkspaceResponse(workspace.Id, workspace.Name, "Owner"));
    }

    [HttpPatch("{workspaceId:guid}")]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        Guid workspaceId,
        [FromBody] UpdateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var workspace = await sender.Send(new UpdateWorkspaceCommand(workspaceId, CurrentUserId, request.Name), cancellationToken);

        return Ok(new WorkspaceResponse(workspace.Id, workspace.Name, "Owner"));
    }
}
