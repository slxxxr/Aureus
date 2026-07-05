using Aureus.Api.Contracts.Transfers;
using Aureus.Api.Filters;
using Aureus.Domain.Workspaces;
using Aureus.UseCases.Transfers.CreateTransfer;
using Aureus.UseCases.Transfers.DeleteTransfer;
using Aureus.UseCases.Transfers.GetTransfers;
using Aureus.UseCases.Transfers.UpdateTransfer;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Transfers;

[RequireWorkspaceRole(WorkspaceRole.Member)]
[Route("api/workspaces/{workspaceId:guid}/transfers")]
public sealed class TransfersController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransferResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var transfers = await sender.Send(new GetTransfersQuery(workspaceId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<TransferResponse>>(transfers));
    }

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

    [HttpPatch("{transferId:guid}")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        Guid workspaceId,
        Guid transferId,
        [FromBody] UpdateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTransferCommand(
            transferId,
            workspaceId,
            CurrentUserId,
            CurrentWorkspaceMembership.Role,
            request.AmountMinor,
            request.OccurredAt,
            request.Note);

        var transfer = await sender.Send(command, cancellationToken);

        return Ok(mapper.Map<TransferResponse>(transfer));
    }

    [HttpDelete("{transferId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid transferId,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTransferCommand(transferId, workspaceId, CurrentUserId, CurrentWorkspaceMembership.Role), cancellationToken);

        return NoContent();
    }
}
