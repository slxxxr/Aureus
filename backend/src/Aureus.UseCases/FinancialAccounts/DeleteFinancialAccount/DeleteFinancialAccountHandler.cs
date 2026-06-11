using Aureus.Domain.FinancialAccounts;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.DeleteFinancialAccount;

public sealed class DeleteFinancialAccountHandler(IFinancialAccountRepository repository)
    : IRequestHandler<DeleteFinancialAccountCommand>
{
    public async Task Handle(DeleteFinancialAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await repository.FindByIdAsync(command.AccountId, command.WorkspaceId, cancellationToken);

        if (account is null)
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.NotFound,
                $"Financial account {command.AccountId} not found.");
        }

        await repository.DeleteAsync(account, cancellationToken);
    }
}
