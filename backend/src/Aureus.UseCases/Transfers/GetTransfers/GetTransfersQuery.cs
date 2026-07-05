using Aureus.Domain.Transfers;
using MediatR;

namespace Aureus.UseCases.Transfers.GetTransfers;

public sealed record GetTransfersQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<Transfer>>;
