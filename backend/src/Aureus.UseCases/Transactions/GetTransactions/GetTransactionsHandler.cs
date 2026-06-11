using Aureus.Domain.Transactions;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transactions.GetTransactions;

public sealed class GetTransactionsHandler(ITransactionRepository repository)
    : IRequestHandler<GetTransactionsQuery, IReadOnlyList<Transaction>>
{
    public Task<IReadOnlyList<Transaction>> Handle(GetTransactionsQuery query, CancellationToken cancellationToken)
    {
        return repository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);
    }
}
