using Aureus.Domain.Transactions;
using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Transactions.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    Guid WorkspaceId,
    Guid RequestingUserId,
    WorkspaceRole RequestingUserRole,
    string? Name,
    long? AmountMinor,
    Guid? CategoryId,
    Guid? FinancialAccountId,
    TransactionType? Type,
    DateOnly? OccurredAt,
    string? Note) : IRequest<Transaction>;
