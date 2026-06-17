using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Transactions.DeleteTransaction;

public sealed record DeleteTransactionCommand(
    Guid TransactionId,
    Guid WorkspaceId,
    Guid RequestingUserId,
    WorkspaceRole RequestingUserRole) : IRequest;
