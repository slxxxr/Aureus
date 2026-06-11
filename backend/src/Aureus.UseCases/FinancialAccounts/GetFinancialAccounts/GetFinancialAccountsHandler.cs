using Aureus.Domain.FinancialAccounts;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.GetFinancialAccounts;

public sealed class GetFinancialAccountsHandler(IFinancialAccountRepository repository)
    : IRequestHandler<GetFinancialAccountsQuery, IReadOnlyList<FinancialAccount>>
{
    public Task<IReadOnlyList<FinancialAccount>> Handle(
        GetFinancialAccountsQuery query,
        CancellationToken cancellationToken)
    {
        return repository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);
    }
}
