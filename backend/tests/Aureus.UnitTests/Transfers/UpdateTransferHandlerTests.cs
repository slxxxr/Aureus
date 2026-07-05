using Aureus.Domain.Transfers;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transfers.UpdateTransfer;

namespace Aureus.UnitTests.Transfers;

public sealed class UpdateTransferHandlerTests
{
    private static Transfer DefaultTransfer(
        Guid? id = null,
        Guid? workspaceId = null,
        Guid? createdByUserId = null,
        long amountMinor = 100_00) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        FromAccountId = Guid.NewGuid(),
        ToAccountId = Guid.NewGuid(),
        CreatedByUserId = createdByUserId ?? Guid.NewGuid(),
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static UpdateTransferCommand OwnerCommand(
        Guid transferId,
        Guid workspaceId,
        long? amountMinor = null,
        DateOnly? occurredAt = null,
        string? note = null) =>
        new(transferId, workspaceId, Guid.NewGuid(), WorkspaceRole.Owner, amountMinor, occurredAt, note);

    [Fact]
    public async Task Handle_TransferNotFound_ThrowsNotFound()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transferRepo = new TransferRepositoryMock().WithNoTransfer(transferId, workspaceId);
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(OwnerCommand(transferId, workspaceId), CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.NotFound, exception.Code);
    }

    [Theory]
    [InlineData(100_00, 200_00, -100_00, 100_00)]
    [InlineData(200_00, 100_00, 100_00, -100_00)]
    public async Task Handle_AmountChanged_PassesCorrectAccountDeltas(
        long oldAmount, long newAmount, long expectedFromDelta, long expectedToDelta)
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, amountMinor: oldAmount);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        await handler.Handle(
            OwnerCommand(transfer.Id, workspaceId, amountMinor: newAmount),
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedFromDelta, transferRepo.UpdatedFromAccountDelta);
        Assert.Equal(expectedToDelta, transferRepo.UpdatedToAccountDelta);
    }

    [Fact]
    public async Task Handle_AmountNull_PassesZeroAccountDeltas()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        await handler.Handle(OwnerCommand(transfer.Id, workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(0, transferRepo.UpdatedFromAccountDelta);
        Assert.Equal(0, transferRepo.UpdatedToAccountDelta);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespaceInNote()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        var result = await handler.Handle(
            OwnerCommand(transfer.Id, workspaceId, note: "  corrected  "),
            CancellationToken.None);

        // Assert
        Assert.Equal("corrected", result.Note);
    }

    [Fact]
    public async Task Handle_NullFields_LeavesFieldsUnchanged()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, amountMinor: 100_00);
        var originalOccurredAt = transfer.OccurredAt;
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        var result = await handler.Handle(OwnerCommand(transfer.Id, workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(100_00, result.AmountMinor);
        Assert.Equal(originalOccurredAt, result.OccurredAt);
    }

    [Fact]
    public async Task Handle_MemberEditsOtherUserTransfer_ThrowsForbidden()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, createdByUserId: ownerId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer);
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                new UpdateTransferCommand(transfer.Id, workspaceId, memberId, WorkspaceRole.Member, null, null, "note"),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.Forbidden, exception.Code);
        transferRepo.VerifyUpdateNotCalled();
    }

    [Fact]
    public async Task Handle_MemberEditsOwnTransfer_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, createdByUserId: memberId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        var result = await handler.Handle(
            new UpdateTransferCommand(transfer.Id, workspaceId, memberId, WorkspaceRole.Member, null, null, "note"),
            CancellationToken.None);

        // Assert
        Assert.Equal("note", result.Note);
    }

    [Fact]
    public async Task Handle_ManagerEditsOtherUserTransfer_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, createdByUserId: Guid.NewGuid());
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = new UpdateTransferHandler(transferRepo.Object);

        // Act
        var result = await handler.Handle(
            new UpdateTransferCommand(transfer.Id, workspaceId, Guid.NewGuid(), WorkspaceRole.Manager, null, null, "note"),
            CancellationToken.None);

        // Assert
        Assert.Equal("note", result.Note);
    }
}
