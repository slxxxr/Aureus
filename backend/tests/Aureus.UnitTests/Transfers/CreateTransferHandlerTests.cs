using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transfers;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transfers.CreateTransfer;

namespace Aureus.UnitTests.Transfers;

public sealed class CreateTransferHandlerTests
{
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

    private static CreateTransferCommand DefaultCommand(
        Guid? workspaceId = null,
        Guid? fromAccountId = null,
        Guid? toAccountId = null,
        long amountMinor = 50_00,
        string? note = null) => new(
        WorkspaceId: workspaceId ?? Guid.NewGuid(),
        FromAccountId: fromAccountId ?? Guid.NewGuid(),
        ToAccountId: toAccountId ?? Guid.NewGuid(),
        CreatedByUserId: Guid.NewGuid(),
        AmountMinor: amountMinor,
        OccurredAt: DateOnly.FromDateTime(DateTime.UtcNow),
        Note: note);

    [Fact]
    public async Task Handle_FromAccountNotFound_ThrowsAccountNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var fromAccountId = Guid.NewGuid();
        var command = DefaultCommand(workspaceId: workspaceId, fromAccountId: fromAccountId);

        var accountRepo = new FinancialAccountRepositoryMock().WithNoAccount(fromAccountId, workspaceId);
        var handler = new CreateTransferHandler(new TransferRepositoryMock().Object, accountRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.AccountNotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_ToAccountNotFound_ThrowsAccountNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var fromAccount = DefaultAccount(workspaceId: workspaceId);
        var toAccountId = Guid.NewGuid();
        var command = DefaultCommand(workspaceId: workspaceId, fromAccountId: fromAccount.Id, toAccountId: toAccountId);

        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(fromAccount.Id, workspaceId, fromAccount)
            .WithNoAccount(toAccountId, workspaceId);
        var handler = new CreateTransferHandler(new TransferRepositoryMock().Object, accountRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.AccountNotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_CurrencyMismatch_ThrowsCurrencyMismatch()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var fromAccount = DefaultAccount(workspaceId: workspaceId, currency: "RUB");
        var toAccount = DefaultAccount(workspaceId: workspaceId, currency: "USD");
        var command = DefaultCommand(workspaceId: workspaceId, fromAccountId: fromAccount.Id, toAccountId: toAccount.Id);

        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(fromAccount.Id, workspaceId, fromAccount)
            .WithAccount(toAccount.Id, workspaceId, toAccount);
        var handler = new CreateTransferHandler(new TransferRepositoryMock().Object, accountRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransferException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(TransferErrorCode.CurrencyMismatch, exception.Code);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsTransferFields()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var fromAccount = DefaultAccount(workspaceId: workspaceId);
        var toAccount = DefaultAccount(workspaceId: workspaceId);
        var command = DefaultCommand(
            workspaceId: workspaceId, fromAccountId: fromAccount.Id, toAccountId: toAccount.Id, amountMinor: 500_00);

        var transferRepo = new TransferRepositoryMock().CapturingAdd();
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(fromAccount.Id, workspaceId, fromAccount)
            .WithAccount(toAccount.Id, workspaceId, toAccount);
        var handler = new CreateTransferHandler(transferRepo.Object, accountRepo.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.WorkspaceId, result.WorkspaceId);
        Assert.Equal(command.FromAccountId, result.FromAccountId);
        Assert.Equal(command.ToAccountId, result.ToAccountId);
        Assert.Equal(command.AmountMinor, result.AmountMinor);
        Assert.Equal(fromAccount.Currency, result.Currency);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespaceInNote()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var fromAccount = DefaultAccount(workspaceId: workspaceId);
        var toAccount = DefaultAccount(workspaceId: workspaceId);
        var command = DefaultCommand(
            workspaceId: workspaceId, fromAccountId: fromAccount.Id, toAccountId: toAccount.Id,
            note: "  moved to savings  ");

        var transferRepo = new TransferRepositoryMock().CapturingAdd();
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(fromAccount.Id, workspaceId, fromAccount)
            .WithAccount(toAccount.Id, workspaceId, toAccount);
        var handler = new CreateTransferHandler(transferRepo.Object, accountRepo.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("moved to savings", result.Note);
    }
}
