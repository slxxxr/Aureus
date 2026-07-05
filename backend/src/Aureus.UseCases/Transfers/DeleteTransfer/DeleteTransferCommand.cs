using Aureus.Domain.Workspaces;
using MediatR;

namespace Aureus.UseCases.Transfers.DeleteTransfer;

public sealed record DeleteTransferCommand(
    Guid TransferId,
    Guid WorkspaceId,
    Guid RequestingUserId,
    WorkspaceRole RequestingUserRole) : IRequest;
