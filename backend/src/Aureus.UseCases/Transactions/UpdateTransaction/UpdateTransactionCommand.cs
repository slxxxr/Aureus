using Aureus.Domain.Transactions;
using MediatR;

namespace Aureus.UseCases.Transactions.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    Guid WorkspaceId,
    string? Name,
    long? AmountMinor,
    Guid? CategoryId,
    Guid? FinancialAccountId,
    TransactionType? Type,
    DateOnly? OccurredAt,
    string? Note) : IRequest<Transaction>;
