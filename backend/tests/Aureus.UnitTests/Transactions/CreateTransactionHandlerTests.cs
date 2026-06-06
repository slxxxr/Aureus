using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.CreateTransaction;

namespace Aureus.UnitTests.Transactions;

public sealed class CreateTransactionHandlerTests
{
    private static FinancialAccount DefaultAccount(Guid? id = null, Guid? workspaceId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        Name = "Cash",
        Currency = "RUB",
        InitialBalanceMinor = 0,
        CurrentBalanceMinor = 10_000_00,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static Category DefaultCategory(Guid? id = null, Guid? workspaceId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorkspaceId = workspaceId ?? Guid.NewGuid(),
        Name = "Groceries",
        Type = TransactionType.Expense,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static CreateTransactionCommand DefaultCommand(
        Guid? workspaceId = null,
        Guid? accountId = null,
        Guid? categoryId = null,
        string name = "Bought bread",
        TransactionType type = TransactionType.Expense,
        long amountMinor = 50_00,
        string? note = null) => new(
        WorkspaceId: workspaceId ?? Guid.NewGuid(),
        FinancialAccountId: accountId ?? Guid.NewGuid(),
        CategoryId: categoryId ?? Guid.NewGuid(),
        CreatedByUserId: Guid.NewGuid(),
        Name: name,
        Type: type,
        AmountMinor: amountMinor,
        OccurredAt: DateTimeOffset.UtcNow,
        Note: note);

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var command = DefaultCommand(workspaceId: workspaceId, accountId: accountId);

        var accountRepo = new FinancialAccountRepositoryMock().WithNoAccount(accountId, workspaceId);
        var handler = new CreateTransactionHandler(
            new TransactionRepositoryMock().Object,
            accountRepo.Object,
            new CategoryRepositoryMock().Object);

        // Act
        var exception = await Assert.ThrowsAsync<FinancialAccountException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(FinancialAccountErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var account = DefaultAccount(workspaceId: workspaceId);
        var categoryId = Guid.NewGuid();
        var command = DefaultCommand(workspaceId: workspaceId, accountId: account.Id, categoryId: categoryId);

        var accountRepo = new FinancialAccountRepositoryMock().WithAccount(account.Id, workspaceId, account);
        var categoryRepo = new CategoryRepositoryMock().WithNoCategory(categoryId, workspaceId);
        var handler = new CreateTransactionHandler(
            new TransactionRepositoryMock().Object,
            accountRepo.Object,
            categoryRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(CategoryErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsTransactionFields()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var account = DefaultAccount(workspaceId: workspaceId);
        var category = DefaultCategory(workspaceId: workspaceId);
        var command = DefaultCommand(
            workspaceId: workspaceId,
            accountId: account.Id,
            categoryId: category.Id,
            type: TransactionType.Income,
            amountMinor: 500_00);

        var transactionRepo = new TransactionRepositoryMock().CapturingAdd();
        var accountRepo = new FinancialAccountRepositoryMock().WithAccount(account.Id, workspaceId, account);
        var categoryRepo = new CategoryRepositoryMock().WithCategory(category.Id, workspaceId, category);
        var handler = new CreateTransactionHandler(transactionRepo.Object, accountRepo.Object, categoryRepo.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.WorkspaceId, result.WorkspaceId);
        Assert.Equal(command.FinancialAccountId, result.FinancialAccountId);
        Assert.Equal(command.CategoryId, result.CategoryId);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Type, result.Type);
        Assert.Equal(command.AmountMinor, result.AmountMinor);
        Assert.Equal(account.Currency, result.Currency);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespace()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var account = DefaultAccount(workspaceId: workspaceId);
        var category = DefaultCategory(workspaceId: workspaceId);
        var command = DefaultCommand(
            workspaceId: workspaceId,
            accountId: account.Id,
            categoryId: category.Id,
            name: "  Bought bread  ",
            note: "  from the store  ");

        var transactionRepo = new TransactionRepositoryMock().CapturingAdd();
        var accountRepo = new FinancialAccountRepositoryMock().WithAccount(account.Id, workspaceId, account);
        var categoryRepo = new CategoryRepositoryMock().WithCategory(category.Id, workspaceId, category);
        var handler = new CreateTransactionHandler(transactionRepo.Object, accountRepo.Object, categoryRepo.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Bought bread", result.Name);
        Assert.Equal("from the store", result.Note);
    }

    [Theory]
    [InlineData(TransactionType.Income, 500_00, 500_00)]
    [InlineData(TransactionType.Expense, 500_00, -500_00)]
    public async Task Handle_ValidCommand_PassesCorrectBalanceDelta(
        TransactionType type, long amountMinor, long expectedDelta)
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var account = DefaultAccount(workspaceId: workspaceId);
        var category = DefaultCategory(workspaceId: workspaceId);
        var command = DefaultCommand(
            workspaceId: workspaceId,
            accountId: account.Id,
            categoryId: category.Id,
            type: type,
            amountMinor: amountMinor);

        var transactionRepo = new TransactionRepositoryMock().CapturingAdd();
        var accountRepo = new FinancialAccountRepositoryMock().WithAccount(account.Id, workspaceId, account);
        var categoryRepo = new CategoryRepositoryMock().WithCategory(category.Id, workspaceId, category);
        var handler = new CreateTransactionHandler(transactionRepo.Object, accountRepo.Object, categoryRepo.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expectedDelta, transactionRepo.SavedBalanceDelta);
    }
}
