using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.UpdateTransaction;

namespace Aureus.UnitTests.Transactions;

public sealed class UpdateTransactionHandlerTests
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
        Name = "Bought bread",
        Type = type,
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transactionRepo = new TransactionRepositoryMock().WithNoTransaction(transactionId, workspaceId);
        var handler = new UpdateTransactionHandler(transactionRepo.Object, new CategoryRepositoryMock().Object);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(
                new UpdateTransactionCommand(transactionId, workspaceId, null, null, null, null, null),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId);
        var newCategoryId = Guid.NewGuid();

        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var categoryRepo = new CategoryRepositoryMock().WithNoCategory(newCategoryId, workspaceId);
        var handler = new UpdateTransactionHandler(transactionRepo.Object, categoryRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            handler.Handle(
                new UpdateTransactionCommand(transaction.Id, workspaceId, null, null, newCategoryId, null, null),
                CancellationToken.None));

        // Assert
        Assert.Equal(CategoryErrorCode.NotFound, exception.Code);
    }

    [Theory]
    [InlineData(TransactionType.Income, 100_00, 200_00, 100_00)]
    [InlineData(TransactionType.Expense, 100_00, 200_00, -100_00)]
    public async Task Handle_AmountChanged_PassesCorrectBalanceDelta(
        TransactionType type, long oldAmount, long newAmount, long expectedDelta)
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, type: type, amountMinor: oldAmount);

        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var handler = new UpdateTransactionHandler(transactionRepo.Object, new CategoryRepositoryMock().Object);

        // Act
        await handler.Handle(
            new UpdateTransactionCommand(transaction.Id, workspaceId, null, newAmount, null, null, null),
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedDelta, transactionRepo.UpdatedBalanceDelta);
    }

    [Fact]
    public async Task Handle_AmountNull_PassesZeroBalanceDelta()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId);

        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var handler = new UpdateTransactionHandler(transactionRepo.Object, new CategoryRepositoryMock().Object);

        // Act
        await handler.Handle(
            new UpdateTransactionCommand(transaction.Id, workspaceId, null, null, null, null, null),
            CancellationToken.None);

        // Assert
        Assert.Equal(0, transactionRepo.UpdatedBalanceDelta);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespace()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId);

        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var handler = new UpdateTransactionHandler(transactionRepo.Object, new CategoryRepositoryMock().Object);

        // Act
        var result = await handler.Handle(
            new UpdateTransactionCommand(transaction.Id, workspaceId, "  Dinner  ", null, null, null, "  at cafe  "),
            CancellationToken.None);

        // Assert
        Assert.Equal("Dinner", result.Name);
        Assert.Equal("at cafe", result.Note);
    }

    [Fact]
    public async Task Handle_NullFields_LeavesFieldsUnchanged()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, amountMinor: 100_00);
        var originalName = transaction.Name;
        var originalCategoryId = transaction.CategoryId;
        var originalOccurredAt = transaction.OccurredAt;

        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var handler = new UpdateTransactionHandler(transactionRepo.Object, new CategoryRepositoryMock().Object);

        // Act
        var result = await handler.Handle(
            new UpdateTransactionCommand(transaction.Id, workspaceId, null, null, null, null, null),
            CancellationToken.None);

        // Assert
        Assert.Equal(originalName, result.Name);
        Assert.Equal(100_00, result.AmountMinor);
        Assert.Equal(originalCategoryId, result.CategoryId);
        Assert.Equal(originalOccurredAt, result.OccurredAt);
    }
}
