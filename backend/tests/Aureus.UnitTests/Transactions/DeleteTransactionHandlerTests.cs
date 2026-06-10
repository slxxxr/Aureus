using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.DeleteTransaction;

namespace Aureus.UnitTests.Transactions;

public sealed class DeleteTransactionHandlerTests
{
    private static Transaction DefaultTransaction(
        Guid? id = null,
        Guid? workspaceId = null,
        TransactionType type = TransactionType.Expense,
        long amountMinor = 100_00) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        FinancialAccountId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        CreatedByUserId = Guid.NewGuid(),
        Type = type,
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transactionRepo = new TransactionRepositoryMock().WithNoTransaction(transactionId, workspaceId);
        var handler = new DeleteTransactionHandler(transactionRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(new DeleteTransactionCommand(transactionId, workspaceId), CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_DoesNotCallDelete()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transactionRepo = new TransactionRepositoryMock().WithNoTransaction(transactionId, workspaceId);
        var handler = new DeleteTransactionHandler(transactionRepo.Object);

        // Act
        await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(new DeleteTransactionCommand(transactionId, workspaceId), CancellationToken.None));

        // Assert
        transactionRepo.VerifyDeleteNotCalled();
    }

    [Theory]
    [InlineData(TransactionType.Income, 500_00, -500_00)]
    [InlineData(TransactionType.Expense, 500_00, 500_00)]
    public async Task Handle_TransactionExists_PassesCorrectBalanceDelta(
        TransactionType type, long amountMinor, long expectedDelta)
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, type: type, amountMinor: amountMinor);

        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingDelete();
        var handler = new DeleteTransactionHandler(transactionRepo.Object);

        // Act
        await handler.Handle(new DeleteTransactionCommand(transaction.Id, workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(expectedDelta, transactionRepo.DeletedBalanceDelta);
        transactionRepo.VerifyDeleteCalledOnce();
        Assert.Equal(transaction.Id, transactionRepo.DeletedTransaction?.Id);
    }
}
