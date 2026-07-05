using Aureus.Domain.FinancialAccounts;
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
        Guid? fromAccountId = null,
        Guid? toAccountId = null,
        Guid? createdByUserId = null,
        long amountMinor = 100_00,
        string currency = "RUB") => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        FromAccountId = fromAccountId ?? Guid.NewGuid(),
        ToAccountId = toAccountId ?? Guid.NewGuid(),
        CreatedByUserId = createdByUserId ?? Guid.NewGuid(),
        AmountMinor = amountMinor,
        Currency = currency,
        OccurredAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static FinancialAccount DefaultAccount(Guid? id = null, Guid? workspaceId = null, string currency = "RUB") => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        Name = "Cash",
        Currency = currency,
        InitialBalanceMinor = 0,
        CurrentBalanceMinor = 10_000_00,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static UpdateTransferHandler BuildHandler(
        TransferRepositoryMock transferRepo,
        FinancialAccountRepositoryMock? accountRepo = null) =>
        new(transferRepo.Object, (accountRepo ?? new FinancialAccountRepositoryMock()).Object);

    private static UpdateTransferCommand OwnerCommand(
        Guid transferId,
        Guid workspaceId,
        Guid? fromAccountId = null,
        Guid? toAccountId = null,
        long? amountMinor = null,
        DateOnly? occurredAt = null,
        string? note = null) =>
        new(transferId, workspaceId, Guid.NewGuid(), WorkspaceRole.Owner,
            fromAccountId, toAccountId, amountMinor, occurredAt, note);

    [Fact]
    public async Task Handle_TransferNotFound_ThrowsNotFound()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transferRepo = new TransferRepositoryMock().WithNoTransfer(transferId, workspaceId);
        var handler = BuildHandler(transferRepo);

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
        var handler = BuildHandler(transferRepo);

        // Act
        await handler.Handle(
            OwnerCommand(transfer.Id, workspaceId, amountMinor: newAmount),
            CancellationToken.None);

        // Assert
        var deltas = transferRepo.UpdatedAccountDeltas!;
        Assert.Equal(expectedFromDelta, deltas[transfer.FromAccountId]);
        Assert.Equal(expectedToDelta, deltas[transfer.ToAccountId]);
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
        var handler = BuildHandler(transferRepo);

        // Act
        await handler.Handle(OwnerCommand(transfer.Id, workspaceId), CancellationToken.None);

        // Assert
        var deltas = transferRepo.UpdatedAccountDeltas!;
        Assert.Equal(0, deltas[transfer.FromAccountId]);
        Assert.Equal(0, deltas[transfer.ToAccountId]);
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
        var handler = BuildHandler(transferRepo);

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
        var originalFromAccountId = transfer.FromAccountId;
        var originalToAccountId = transfer.ToAccountId;
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var handler = BuildHandler(transferRepo);

        // Act
        var result = await handler.Handle(OwnerCommand(transfer.Id, workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(100_00, result.AmountMinor);
        Assert.Equal(originalOccurredAt, result.OccurredAt);
        Assert.Equal(originalFromAccountId, result.FromAccountId);
        Assert.Equal(originalToAccountId, result.ToAccountId);
    }

    [Fact]
    public async Task Handle_FromAccountNotFound_ThrowsAccountNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var newAccountId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer);
        var accountRepo = new FinancialAccountRepositoryMock().WithNoAccount(newAccountId, workspaceId);
        var handler = BuildHandler(transferRepo, accountRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                OwnerCommand(transfer.Id, workspaceId, fromAccountId: newAccountId),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.AccountNotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_ToAccountNotFound_ThrowsAccountNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var newAccountId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer);
        var accountRepo = new FinancialAccountRepositoryMock().WithNoAccount(newAccountId, workspaceId);
        var handler = BuildHandler(transferRepo, accountRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                OwnerCommand(transfer.Id, workspaceId, toAccountId: newAccountId),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.AccountNotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_NewFromAccountEqualsExistingToAccount_ThrowsSameAccount()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId);
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer);
        var handler = BuildHandler(transferRepo);

        // Act — set FromAccountId to the transfer's own (unchanged) ToAccountId
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                OwnerCommand(transfer.Id, workspaceId, fromAccountId: transfer.ToAccountId),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.SameAccount, exception.Code);
        transferRepo.VerifyUpdateNotCalled();
    }

    [Fact]
    public async Task Handle_NewAccountsCurrencyMismatch_ThrowsCurrencyMismatch()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transfer = DefaultTransfer(workspaceId: workspaceId, currency: "RUB");
        var newFromAccount = DefaultAccount(workspaceId: workspaceId, currency: "USD");
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer);
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(newFromAccount.Id, workspaceId, newFromAccount);
        var handler = BuildHandler(transferRepo, accountRepo);

        // Act — new FromAccount is USD, but ToAccount (unchanged) stays RUB
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                OwnerCommand(transfer.Id, workspaceId, fromAccountId: newFromAccount.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.CurrencyMismatch, exception.Code);
    }

    [Fact]
    public async Task Handle_FromAccountChanged_UpdatesFromAccountIdAndReversesOldBalance()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var oldFromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var transfer = DefaultTransfer(
            workspaceId: workspaceId, fromAccountId: oldFromAccountId, toAccountId: toAccountId, amountMinor: 100_00);
        var newFromAccount = DefaultAccount(workspaceId: workspaceId, currency: "RUB");
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(newFromAccount.Id, workspaceId, newFromAccount);
        var handler = BuildHandler(transferRepo, accountRepo);

        // Act
        var result = await handler.Handle(
            OwnerCommand(transfer.Id, workspaceId, fromAccountId: newFromAccount.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(newFromAccount.Id, result.FromAccountId);
        var deltas = transferRepo.UpdatedAccountDeltas!;
        Assert.Equal(100_00, deltas[oldFromAccountId]);   // reverse old debit
        Assert.Equal(-100_00, deltas[newFromAccount.Id]); // apply new debit
        Assert.Equal(0, deltas[toAccountId]);              // to-account unaffected
    }

    [Fact]
    public async Task Handle_DirectionReversed_ProducesDoubleDeltaOnBothAccounts()
    {
        // Arrange — reverse an A->B transfer into B->A
        var workspaceId = Guid.NewGuid();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var transfer = DefaultTransfer(
            workspaceId: workspaceId, fromAccountId: accountA, toAccountId: accountB, amountMinor: 100_00);
        var accountBEntity = DefaultAccount(id: accountB, workspaceId: workspaceId, currency: "RUB");
        var accountAEntity = DefaultAccount(id: accountA, workspaceId: workspaceId, currency: "RUB");
        var transferRepo = new TransferRepositoryMock()
            .WithTransfer(transfer.Id, workspaceId, transfer)
            .CapturingUpdate();
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(accountB, workspaceId, accountBEntity)
            .WithAccount(accountA, workspaceId, accountAEntity);
        var handler = BuildHandler(transferRepo, accountRepo);

        // Act
        var result = await handler.Handle(
            OwnerCommand(transfer.Id, workspaceId, fromAccountId: accountB, toAccountId: accountA),
            CancellationToken.None);

        // Assert
        Assert.Equal(accountB, result.FromAccountId);
        Assert.Equal(accountA, result.ToAccountId);
        var deltas = transferRepo.UpdatedAccountDeltas!;
        Assert.Equal(200_00, deltas[accountA]);
        Assert.Equal(-200_00, deltas[accountB]);
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
        var handler = BuildHandler(transferRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(
                new UpdateTransferCommand(transfer.Id, workspaceId, memberId, WorkspaceRole.Member, null, null, null, null, "note"),
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
        var handler = BuildHandler(transferRepo);

        // Act
        var result = await handler.Handle(
            new UpdateTransferCommand(transfer.Id, workspaceId, memberId, WorkspaceRole.Member, null, null, null, null, "note"),
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
        var handler = BuildHandler(transferRepo);

        // Act
        var result = await handler.Handle(
            new UpdateTransferCommand(transfer.Id, workspaceId, Guid.NewGuid(), WorkspaceRole.Manager, null, null, null, null, "note"),
            CancellationToken.None);

        // Assert
        Assert.Equal("note", result.Note);
    }
}
