using Aureus.Domain.Transfers;
using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Transfers.UpdateTransfer;

public sealed record UpdateTransferCommand(
    Guid TransferId,
    Guid WorkspaceId,
    Guid RequestingUserId,
    WorkspaceRole RequestingUserRole,
    Guid? FromAccountId,
    Guid? ToAccountId,
    long? AmountMinor,
    DateOnly? OccurredAt,
    string? Note) : IRequest<Transfer>;
