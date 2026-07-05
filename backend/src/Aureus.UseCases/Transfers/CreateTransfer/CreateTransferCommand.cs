using Aureus.Domain.Transfers;
using MediatR;

namespace Aureus.UseCases.Transfers.CreateTransfer;

public sealed record CreateTransferCommand(
    Guid WorkspaceId,
    Guid FromAccountId,
    Guid ToAccountId,
    Guid CreatedByUserId,
    long AmountMinor,
    DateOnly OccurredAt,
    string? Note) : IRequest<Transfer>;
