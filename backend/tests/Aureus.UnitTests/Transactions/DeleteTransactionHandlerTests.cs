using Aureus.Domain.Transactions;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.DeleteTransaction;

namespace Aureus.UnitTests.Transactions;

public sealed class DeleteTransactionHandlerTests
{
    private static Transaction DefaultTransaction(
        Guid? id = null,
        Guid? workspaceId = null,
        Guid? createdByUserId = null,
        TransactionType type = TransactionType.Expense,
        long amountMinor = 100_00) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        FinancialAccountId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        CreatedByUserId = createdByUserId ?? Guid.NewGuid(),
        Type = type,
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static DeleteTransactionCommand OwnerCommand(Guid transactionId, Guid workspaceId) =>
        new(transactionId, workspaceId, Guid.NewGuid(), WorkspaceRole.Owner);

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
            handler.Handle(OwnerCommand(transactionId, workspaceId), CancellationToken.None));

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
            handler.Handle(OwnerCommand(transactionId, workspaceId), CancellationToken.None));

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
        await handler.Handle(OwnerCommand(transaction.Id, workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(expectedDelta, transactionRepo.DeletedBalanceDelta);
        transactionRepo.VerifyDeleteCalledOnce();
        Assert.Equal(transaction.Id, transactionRepo.DeletedTransaction?.Id);
    }

    [Fact]
    public async Task Handle_MemberDeletesOtherUserTransaction_ThrowsForbidden()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, createdByUserId: ownerId);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction);
        var handler = new DeleteTransactionHandler(transactionRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(
                new DeleteTransactionCommand(transaction.Id, workspaceId, memberId, WorkspaceRole.Member),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.Forbidden, exception.Code);
        transactionRepo.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_MemberDeletesOwnTransaction_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, createdByUserId: memberId);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingDelete();
        var handler = new DeleteTransactionHandler(transactionRepo.Object);

        // Act
        await handler.Handle(
            new DeleteTransactionCommand(transaction.Id, workspaceId, memberId, WorkspaceRole.Member),
            CancellationToken.None);

        // Assert
        transactionRepo.VerifyDeleteCalledOnce();
    }

    [Fact]
    public async Task Handle_ManagerDeletesOtherUserTransaction_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, createdByUserId: Guid.NewGuid());
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingDelete();
        var handler = new DeleteTransactionHandler(transactionRepo.Object);

        // Act
        await handler.Handle(
            new DeleteTransactionCommand(transaction.Id, workspaceId, Guid.NewGuid(), WorkspaceRole.Manager),
            CancellationToken.None);

        // Assert
        transactionRepo.VerifyDeleteCalledOnce();
    }
}
