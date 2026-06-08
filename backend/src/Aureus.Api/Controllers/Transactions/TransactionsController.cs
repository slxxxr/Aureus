using Aureus.Api.Contracts.Transactions;
using Aureus.Api.Filters;
using Aureus.UseCases.Transactions.CreateTransaction;
using Aureus.UseCases.Transactions.DeleteTransaction;
using Aureus.UseCases.Transactions.GetTransactions;
using Aureus.UseCases.Transactions.UpdateTransaction;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.Transactions;

[ValidateWorkspaceMember]
[Route("api/workspaces/{workspaceId:guid}/transactions")]
public sealed class TransactionsController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var transactions = await sender.Send(new GetTransactionsQuery(workspaceId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<TransactionResponse>>(transactions));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(
        Guid workspaceId,
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransactionCommand(
            workspaceId,
            request.FinancialAccountId,
            request.CategoryId,
            CurrentUserId,
            request.Name,
            request.Type,
            request.AmountMinor,
            request.OccurredAt,
            request.Note);

        var transaction = await sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, mapper.Map<TransactionResponse>(transaction));
    }

    [HttpPatch("{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        Guid workspaceId,
        Guid transactionId,
        [FromBody] UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTransactionCommand(
            transactionId,
            workspaceId,
            request.Name,
            request.AmountMinor,
            request.CategoryId,
            request.FinancialAccountId,
            request.Type,
            request.OccurredAt,
            request.Note);

        var transaction = await sender.Send(command, cancellationToken);

        return Ok(mapper.Map<TransactionResponse>(transaction));
    }

    [HttpDelete("{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTransactionCommand(transactionId, workspaceId), cancellationToken);

        return NoContent();
    }
}
