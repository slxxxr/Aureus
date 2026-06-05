using Aureus.Api.Contracts.Workspaces;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
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

        var response = workspaces
            .Select(w => new WorkspaceResponse(w.Id, w.Name, w.Role))
            .ToList();

        return Ok(response);
    }
}
