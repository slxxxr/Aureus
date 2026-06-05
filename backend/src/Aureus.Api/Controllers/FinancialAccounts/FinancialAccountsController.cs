using Aureus.Api.Contracts.FinancialAccounts;
using Aureus.Api.Filters;
using Aureus.UseCases.FinancialAccounts.CreateFinancialAccount;
using Aureus.UseCases.FinancialAccounts.DeleteFinancialAccount;
using Aureus.UseCases.FinancialAccounts.GetFinancialAccounts;
using Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aureus.Api.Controllers.FinancialAccounts;

[ValidateWorkspaceMember]
[Route("api/workspaces/{workspaceId:guid}/financial-accounts")]
public sealed class FinancialAccountsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinancialAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var accounts = await sender.Send(new GetFinancialAccountsQuery(workspaceId), cancellationToken);

        var response = accounts
            .Select(a => new FinancialAccountResponse(
                a.Id, a.Name, a.Currency,
                a.InitialBalanceMinor, a.CurrentBalanceMinor,
                a.CreatedAt, a.UpdatedAt))
            .ToList();

        return Ok(response);
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

        var response = new FinancialAccountResponse(
            account.Id, account.Name, account.Currency,
            account.InitialBalanceMinor, account.CurrentBalanceMinor,
            account.CreatedAt, account.UpdatedAt);

        return StatusCode(StatusCodes.Status201Created, response);
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

        var response = new FinancialAccountResponse(
            account.Id, account.Name, account.Currency,
            account.InitialBalanceMinor, account.CurrentBalanceMinor,
            account.CreatedAt, account.UpdatedAt);

        return Ok(response);
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
