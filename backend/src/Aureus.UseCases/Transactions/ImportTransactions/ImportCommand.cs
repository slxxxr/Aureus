using MediatR;

namespace Aureus.UseCases.Transactions.ImportTransactions;

public sealed record ImportCommand(
    Guid WorkspaceId,
    Guid UserId,
    byte[] FileContent) : IRequest<int>;
