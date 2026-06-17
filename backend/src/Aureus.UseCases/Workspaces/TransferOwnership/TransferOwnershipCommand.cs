using MediatR;

namespace Aureus.UseCases.Workspaces.TransferOwnership;

public sealed record TransferOwnershipCommand(
    Guid WorkspaceId,
    Guid FromUserId,
    Guid ToUserId) : IRequest;
