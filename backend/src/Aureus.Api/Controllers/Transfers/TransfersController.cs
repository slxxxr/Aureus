using Aureus.Api.Contracts.Transfers;
using Aureus.Api.Filters;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Transfers.CreateTransfer;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Transfers;

[RequireWorkspaceRole(WorkspaceRole.Member)]
[Route("api/workspaces/{workspaceId:guid}/transfers")]
public sealed class TransfersController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(
        Guid workspaceId,
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransferCommand(
            workspaceId,
            request.FromAccountId,
            request.ToAccountId,
            CurrentUserId,
            request.AmountMinor,
            request.OccurredAt,
            request.Note);

        var transfer = await sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, mapper.Map<TransferResponse>(transfer));
    }
}
