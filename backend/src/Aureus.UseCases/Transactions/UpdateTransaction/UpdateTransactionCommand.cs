using MediatR;

namespace Aureus.UseCases.Transactions.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    Guid WorkspaceId,
    long? AmountMinor,
    Guid? CategoryId,
    DateTimeOffset? OccurredAt,
    string? Note) : IRequest<Domain.Transactions.Transaction>;
