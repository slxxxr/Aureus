using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Domain.Workspaces;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.UpdateTransaction;

namespace Aureus.UnitTests.Transactions;

public sealed class UpdateTransactionHandlerTests
{
    private static Transaction DefaultTransaction(
        Guid? id = null,
        Guid? workspaceId = null,
        Guid? accountId = null,
        Guid? createdByUserId = null,
        TransactionType type = TransactionType.Expense,
        long amountMinor = 100_00,
        string currency = "RUB") => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        FinancialAccountId = accountId ?? Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        CreatedByUserId = createdByUserId ?? Guid.NewGuid(),
        Name = "Bought bread",
        Type = type,
        AmountMinor = amountMinor,
        Currency = currency,
        OccurredAt = DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static Category DefaultCategory(
        Guid? id = null,
        Guid? workspaceId = null,
        TransactionType type = TransactionType.Expense) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        Name = "Food",
        Type = type,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static FinancialAccount DefaultAccount(
        Guid? id = null,
        Guid? workspaceId = null,
        string currency = "RUB") => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        Name = "Cash",
        Currency = currency,
        InitialBalanceMinor = 0,
        CurrentBalanceMinor = 0,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static UpdateTransactionHandler BuildHandler(
        TransactionRepositoryMock transactionRepo,
        CategoryRepositoryMock? categoryRepo = null,
        FinancialAccountRepositoryMock? accountRepo = null) =>
        new(transactionRepo.Object,
            (categoryRepo ?? new CategoryRepositoryMock()).Object,
            (accountRepo ?? new FinancialAccountRepositoryMock()).Object);

    private static UpdateTransactionCommand OwnerCommand(
        Guid transactionId,
        Guid workspaceId,
        string? name = null,
        long? amountMinor = null,
        Guid? categoryId = null,
        Guid? financialAccountId = null,
        TransactionType? type = null,
        DateOnly? occurredAt = null,
        string? note = null) =>
        new(transactionId, workspaceId, Guid.NewGuid(), WorkspaceRole.Owner,
            name, amountMinor, categoryId, financialAccountId, type, occurredAt, note);

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var transactionRepo = new TransactionRepositoryMock().WithNoTransaction(transactionId, workspaceId);
        var handler = BuildHandler(transactionRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(OwnerCommand(transactionId, workspaceId), CancellationToken.None));

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
        var handler = BuildHandler(transactionRepo, categoryRepo);

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            handler.Handle(
                OwnerCommand(transaction.Id, workspaceId, categoryId: newCategoryId),
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
        var handler = BuildHandler(transactionRepo);

        // Act
        await handler.Handle(
            OwnerCommand(transaction.Id, workspaceId, amountMinor: newAmount),
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
        var handler = BuildHandler(transactionRepo);

        // Act
        await handler.Handle(OwnerCommand(transaction.Id, workspaceId), CancellationToken.None);

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
        var handler = BuildHandler(transactionRepo);

        // Act
        var result = await handler.Handle(
            OwnerCommand(transaction.Id, workspaceId, name: "  Dinner  ", note: "  at cafe  "),
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
        var handler = BuildHandler(transactionRepo);

        // Act
        var result = await handler.Handle(OwnerCommand(transaction.Id, workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(originalName, result.Name);
        Assert.Equal(100_00, result.AmountMinor);
        Assert.Equal(originalCategoryId, result.CategoryId);
        Assert.Equal(originalOccurredAt, result.OccurredAt);
    }

    [Fact]
    public async Task Handle_TypeChanged_WithoutCategoryId_ThrowsCategoryRequired()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, type: TransactionType.Expense);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction);
        var handler = BuildHandler(transactionRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(
                OwnerCommand(transaction.Id, workspaceId, type: TransactionType.Income),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.CategoryRequiredOnTypeChange, exception.Code);
    }

    [Fact]
    public async Task Handle_TypeChanged_CategoryTypeMismatch_ThrowsCategoryTypeMismatch()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, type: TransactionType.Expense);
        var expenseCategory = DefaultCategory(workspaceId: workspaceId, type: TransactionType.Expense);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction);
        var categoryRepo = new CategoryRepositoryMock()
            .WithCategory(expenseCategory.Id, workspaceId, expenseCategory);
        var handler = BuildHandler(transactionRepo, categoryRepo);

        // Act — new type is Income but category is Expense
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(
                OwnerCommand(transaction.Id, workspaceId, categoryId: expenseCategory.Id, type: TransactionType.Income),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.CategoryTypeMismatch, exception.Code);
    }

    [Theory]
    [InlineData(TransactionType.Expense, TransactionType.Income, 100_00, 200_00)]
    [InlineData(TransactionType.Income, TransactionType.Expense, 100_00, -200_00)]
    public async Task Handle_TypeChanged_PassesCorrectBalanceDelta(
        TransactionType oldType, TransactionType newType, long amount, long expectedDelta)
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, type: oldType, amountMinor: amount);
        var newCategory = DefaultCategory(workspaceId: workspaceId, type: newType);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var categoryRepo = new CategoryRepositoryMock()
            .WithCategory(newCategory.Id, workspaceId, newCategory);
        var handler = BuildHandler(transactionRepo, categoryRepo);

        // Act
        await handler.Handle(
            OwnerCommand(transaction.Id, workspaceId, categoryId: newCategory.Id, type: newType),
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedDelta, transactionRepo.UpdatedBalanceDelta);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var newAccountId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction);
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithNoAccount(newAccountId, workspaceId);
        var handler = BuildHandler(transactionRepo, accountRepo: accountRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(
                OwnerCommand(transaction.Id, workspaceId, financialAccountId: newAccountId),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.AccountNotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_AccountChanged_UpdatesCurrencyAndAccountId()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, currency: "RUB");
        var newAccount = DefaultAccount(workspaceId: workspaceId, currency: "USD");
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(newAccount.Id, workspaceId, newAccount);
        var handler = BuildHandler(transactionRepo, accountRepo: accountRepo);

        // Act
        var result = await handler.Handle(
            OwnerCommand(transaction.Id, workspaceId, financialAccountId: newAccount.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(newAccount.Id, result.FinancialAccountId);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task Handle_AccountChanged_PassesOldAndNewAccountDeltas()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var oldAccountId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, accountId: oldAccountId, type: TransactionType.Expense, amountMinor: 100_00);
        var newAccount = DefaultAccount(workspaceId: workspaceId);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var accountRepo = new FinancialAccountRepositoryMock()
            .WithAccount(newAccount.Id, workspaceId, newAccount);
        var handler = BuildHandler(transactionRepo, accountRepo: accountRepo);

        // Act
        await handler.Handle(
            OwnerCommand(transaction.Id, workspaceId, financialAccountId: newAccount.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(oldAccountId, transactionRepo.UpdatedOldAccountId);
        Assert.Equal(100_00, transactionRepo.UpdatedOldAccountDelta);
        Assert.Equal(-100_00, transactionRepo.UpdatedNewAccountDelta);
    }

    [Fact]
    public async Task Handle_MemberEditsOtherUserTransaction_ThrowsForbidden()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, createdByUserId: ownerId);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction);
        var handler = BuildHandler(transactionRepo);

        // Act
        var exception = await Assert.ThrowsAsync<TransactionException>(() =>
            handler.Handle(
                new UpdateTransactionCommand(
                    transaction.Id, workspaceId, memberId, WorkspaceRole.Member,
                    "New name", null, null, null, null, null, null),
                CancellationToken.None));

        // Assert
        Assert.Equal(TransactionErrorCode.Forbidden, exception.Code);
    }

    [Fact]
    public async Task Handle_MemberEditsOwnTransaction_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, createdByUserId: memberId);
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var handler = BuildHandler(transactionRepo);

        // Act
        var result = await handler.Handle(
            new UpdateTransactionCommand(
                transaction.Id, workspaceId, memberId, WorkspaceRole.Member,
                "New name", null, null, null, null, null, null),
            CancellationToken.None);

        // Assert
        Assert.Equal("New name", result.Name);
    }

    [Fact]
    public async Task Handle_ManagerEditsOtherUserTransaction_Succeeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var transaction = DefaultTransaction(workspaceId: workspaceId, createdByUserId: Guid.NewGuid());
        var transactionRepo = new TransactionRepositoryMock()
            .WithTransaction(transaction.Id, workspaceId, transaction)
            .CapturingUpdate();
        var handler = BuildHandler(transactionRepo);

        // Act
        var result = await handler.Handle(
            new UpdateTransactionCommand(
                transaction.Id, workspaceId, Guid.NewGuid(), WorkspaceRole.Manager,
                "New name", null, null, null, null, null, null),
            CancellationToken.None);

        // Assert
        Assert.Equal("New name", result.Name);
    }
}
