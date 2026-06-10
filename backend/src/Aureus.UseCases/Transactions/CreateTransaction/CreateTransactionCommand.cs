using Aureus.Domain.Transactions;
using MediatR;

namespace Aureus.UseCases.Transactions.CreateTransaction;

public sealed record CreateTransactionCommand(
    Guid WorkspaceId,
    Guid FinancialAccountId,
    Guid CategoryId,
    Guid CreatedByUserId,
    string Name,
    TransactionType Type,
    long AmountMinor,
    DateOnly OccurredAt,
    string? Note) : IRequest<Transaction>;
