using Aureus.Domain.Transfers;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transfers.DeleteTransfer;

namespace Aureus.UnitTests.Transfers;

public sealed class DeleteTransferHandlerTests
{
    private static Transfer DefaultTransfer(
        Guid? id = null,
        Guid? workspaceId = null,
        Guid? createdByUserId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        FromAccountId = Guid.NewGuid(),
        ToAccountId = Guid.NewGuid(),
        CreatedByUserId = createdByUserId ?? Guid.NewGuid(),
        AmountMinor = 100_00,
        Currency = "RUB",
        OccurredAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static DeleteTransferCommand OwnerCommand(Guid transferId, Guid workspaceId) =>
        new(transferId, workspaceId, Guid.NewGuid(), WorkspaceRole.Owner);

    [Fact]
    public async Task Handle_TransferNotFound_ThrowsNotFound()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transferRepo = new TransferRepositoryMock().WithNoTransfer(transferId, workspaceId);
        var handler = new DeleteTransferHandler(transferRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(OwnerCommand(transferId, workspaceId), CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_TransferNotFound_DoesNotCallDelete()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transferRepo = new TransferRepositoryMock().WithNoTransfer(transferId, workspaceId);
        var handler = new DeleteTransferHandler(transferRepo.Object);

        // Act
        await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(OwnerCommand(transferId, workspaceId), CancellationToken.None));

        // Assert
        transferRepo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_TransferExists_CallsDeleteWithTransfer()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingDelete();
        var handler = new DeleteTransferHandler(transferRepo.Object);

        // Act
        await handler.Handle(OwnerCommand(transfer.Id, workspaceId), CancellationToken.None);

        // Assert
        transferRepo.VerifyDeleteCalledOnce();
        Assert.Equal(transfer.Id, transferRepo.DeletedTransfer?.Id);
    }

    [Fact]
    public async Task Handle_MemberDeletesOtherUserTransfer_ThrowsForbidden()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, createdByUserId: ownerId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer);
        var handler = new DeleteTransferHandler(transferRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                new DeleteTransferCommand(transfer.Id, workspaceId, memberId, WorkspaceRole.Member),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.Forbidden, exception.Code);
        transferRepo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_MemberDeletesOwnTransfer_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, createdByUserId: memberId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingDelete();
        var handler = new DeleteTransferHandler(transferRepo.Object);

        // Act
        await handler.Handle(
            new DeleteTransferCommand(transfer.Id, workspaceId, memberId, WorkspaceRole.Member),
            CancellationToken.None);

        // Assert
        transferRepo.VerifyDeleteCalledOnce();
    }

    [Fact]
    public async Task Handle_ManagerDeletesOtherUserTransfer_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, createdByUserId: Guid.NewGuid());
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingDelete();
        var handler = new DeleteTransferHandler(transferRepo.Object);

        // Act
        await handler.Handle(
            new DeleteTransferCommand(transfer.Id, workspaceId, Guid.NewGuid(), WorkspaceRole.Manager),
            CancellationToken.None);

        // Assert
        transferRepo.VerifyDeleteCalledOnce();
    }
}
