using Aureus.Domain.Transfers;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Transfers.GetTransfers;

public sealed class GetTransfersHandler(ITransferRepository repository)
    : IRequestHandler<GetTransfersQuery, IReadOnlyList<Transfer>>
{
    public Task<IReadOnlyList<Transfer>> Handle(GetTransfersQuery query, CancellationToken cancellationToken)
    {
        return repository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);
    }
}
