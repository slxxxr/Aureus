using Aureus.Domain.Transactions;
using MediatR;

namespace Aureus.UseCases.Transactions.CreateTransaction;

public sealed record CreateTransactionCommand(
    Guid WorkspaceId,
    Guid FinancialAccountId,
    Guid CategoryId,
    Guid CreatedByUserId,
    TransactionType Type,
    long AmountMinor,
    DateTimeOffset OccurredAt,
    string? Note) : IRequest<Transaction>;
