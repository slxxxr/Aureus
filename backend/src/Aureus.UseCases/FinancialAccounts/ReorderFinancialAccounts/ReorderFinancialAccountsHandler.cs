using Aureus.Domain.FinancialAccounts;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.ReorderFinancialAccounts;

public sealed class ReorderFinancialAccountsHandler(IFinancialAccountRepository repository)
    : IRequestHandler<ReorderFinancialAccountsCommand>
{
    public Task Handle(ReorderFinancialAccountsCommand command, CancellationToken cancellationToken)
    {
        if (!command.AccountIds.Order().SequenceEqual(command.ExpectedAccountIds.Order()))
        {
            throw new FinancialAccountException(
                FinancialAccountErrorCode.InvalidOrder, "The two orders must contain the same account IDs.");
        }

        return repository.ReorderAsync(command.WorkspaceId, command.AccountIds, command.ExpectedAccountIds,
            cancellationToken);
    }
}
