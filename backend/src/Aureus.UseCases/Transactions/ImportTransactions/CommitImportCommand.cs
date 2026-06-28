using MediatR;

namespace Aureus.UseCases.Transactions.ImportTransactions;

public sealed record CommitImportCommand(
    Guid WorkspaceId,
    Guid UserId,
    byte[] FileContent) : IRequest<int>;
