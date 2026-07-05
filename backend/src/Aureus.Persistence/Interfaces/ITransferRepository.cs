using Aureus.Domain.Transfers;

namespace Aureus.Persistence.Interfaces;

public interface ITransferRepository
{
    Task AddAsync(Transfer transfer, CancellationToken cancellationToken);
}
