using Aureus.Api.Contracts.FinancialAccounts;
using Aureus.Api.Filters;
using Aureus.UseCases.FinancialAccounts.CreateFinancialAccount;
using Aureus.UseCases.FinancialAccounts.DeleteFinancialAccount;
using Aureus.UseCases.FinancialAccounts.GetFinancialAccounts;
using Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.FinancialAccounts;

[ValidateWorkspaceMember]
[Route("api/workspaces/{workspaceId:guid}/financial-accounts")]
public sealed class FinancialAccountsController(ISender sender, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinancialAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var accounts = await sender.Send(new GetFinancialAccountsQuery(workspaceId), cancellationToken);

        return Ok(mapper.Map<IReadOnlyList<FinancialAccountResponse>>(accounts));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FinancialAccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(
        Guid workspaceId,
        [FromBody] CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFinancialAccountCommand(
            workspaceId, request.Name, request.Currency, request.InitialBalanceMinor);

        var account = await sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, mapper.Map<FinancialAccountResponse>(account));
    }

    [HttpPatch("{accountId:guid}")]
    [ProducesResponseType(typeof(FinancialAccountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        Guid workspaceId,
        Guid accountId,
        [FromBody] UpdateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFinancialAccountCommand(
            accountId, workspaceId, request.Name, request.InitialBalanceMinor);

        var account = await sender.Send(command, cancellationToken);

        return Ok(mapper.Map<FinancialAccountResponse>(account));
    }

    [HttpDelete("{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteFinancialAccountCommand(accountId, workspaceId), cancellationToken);

        return NoContent();
    }
}
