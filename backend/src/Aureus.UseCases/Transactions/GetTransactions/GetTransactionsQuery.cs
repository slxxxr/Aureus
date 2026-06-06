using Aureus.Domain.Transactions;
using MediatR;

namespace Aureus.UseCases.Transactions.GetTransactions;

public sealed record GetTransactionsQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<Transaction>>;
