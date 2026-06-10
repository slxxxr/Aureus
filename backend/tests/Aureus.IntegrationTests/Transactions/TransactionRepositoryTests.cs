using Aureus.Domain.Transactions;
using Aureus.IntegrationTests.Common;
using Aureus.Postgres.Implementations.Transactions;

namespace Aureus.IntegrationTests.Transactions;

[Collection(nameof(PostgresCollection))]
public sealed class TransactionRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidTransaction_PersistsTransaction()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transaction = NewTransaction(workspaceId, accountId, categoryId, userId, "Bought bread", TransactionType.Expense, 50_00);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new TransactionRepository(db, fixture.Mapper)
                .AddAsync(transaction, -50_00, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransactionRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transaction.Id, workspaceId, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(transaction.Name, stored!.Name);
        Assert.Equal(transaction.Type, stored.Type);
        Assert.Equal(transaction.AmountMinor, stored.AmountMinor);
        Assert.Equal(transaction.CategoryId, stored.CategoryId);
        Assert.Equal(transaction.FinancialAccountId, stored.FinancialAccountId);
    }

    [Theory]
    [InlineData(TransactionType.Income, 50_00, 150_00)]
    [InlineData(TransactionType.Expense, 50_00, 50_00)]
    public async Task AddAsync_ValidTransaction_UpdatesAccountBalance(
        TransactionType type, long amountMinor, long expectedBalance)
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var balanceDelta = type == TransactionType.Income ? amountMinor : -amountMinor;
        var transaction = NewTransaction(workspaceId, accountId, categoryId, userId, "Transaction", type, amountMinor);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new TransactionRepository(db, fixture.Mapper)
                .AddAsync(transaction, balanceDelta, CancellationToken.None);
        }

        // Assert
        var balance = await GetAccountBalanceAsync(accountId, workspaceId);
        Assert.Equal(expectedBalance, balance);
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedTransaction_ReturnsNull()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);

        // Act
        await DeleteTransactionAsync(transactionId, workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransactionRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task FindByIdAsync_WrongWorkspace_ReturnsNull()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var (otherWorkspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new TransactionRepository(db, fixture.Mapper)
            .FindByIdAsync(transactionId, otherWorkspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_SoftDeletedTransaction_ExcludesIt()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var liveId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);
        var deletedId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);
        await DeleteTransactionAsync(deletedId, workspaceId);

        // Act
        await using var db = fixture.CreateDbContext();
        var transactions = await new TransactionRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Single(transactions);
        Assert.Equal(liveId, transactions[0].Id);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_ReturnsTransactionsOrderedByOccurredAtDescending()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var olderId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: now.AddDays(-2));
        var newerId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: now);

        // Act
        await using var db = fixture.CreateDbContext();
        var transactions = await new TransactionRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Equal([newerId, olderId], transactions.Select(t => t.Id));
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_SameDay_OrdersByCreatedAtDescending()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: day);
        var secondId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: day);
        var thirdId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: day);
        var fourthId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: day);

        // Act
        await using var db = fixture.CreateDbContext();
        var transactions = await new TransactionRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Equal([fourthId, thirdId, secondId, firstId], transactions.Select(t => t.Id));
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_AcrossTwoDays_OrdersByDayThenCreatedAt()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var earlierDay = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var laterDay = DateOnly.FromDateTime(DateTime.UtcNow);

        var earlierFirst = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: earlierDay);
        var laterFirst = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: laterDay);
        var earlierSecond = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: earlierDay);
        var laterSecond = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, occurredAt: laterDay);

        // Act
        await using var db = fixture.CreateDbContext();
        var transactions = await new TransactionRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Equal(
            [laterSecond, laterFirst, earlierSecond, earlierFirst],
            transactions.Select(t => t.Id));
    }

    [Fact]
    public async Task UpdateAsync_ValidTransaction_UpdatesFields()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var newCategoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, name: "Bought bread");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new TransactionRepository(db, fixture.Mapper);
            var transaction = await repo.FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
            transaction!.Name = "Dinner";
            transaction.Note = "at cafe";
            transaction.CategoryId = newCategoryId;
            transaction.UpdatedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(transaction, transaction.FinancialAccountId, 0, 0, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransactionRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transactionId, workspaceId, CancellationToken.None);

        Assert.Equal("Dinner", stored!.Name);
        Assert.Equal("at cafe", stored.Note);
        Assert.Equal(newCategoryId, stored.CategoryId);
    }

    [Fact]
    public async Task UpdateAsync_AmountChanged_UpdatesAccountBalance()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 0);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await AddTransactionAsync(
            workspaceId, accountId, categoryId, userId,
            type: TransactionType.Income, amountMinor: 100_00);

        // Act — update amount from 100 to 200, balanceDelta = +100
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new TransactionRepository(db, fixture.Mapper);
            var transaction = await repo.FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
            transaction!.AmountMinor = 200_00;
            transaction.UpdatedAt = DateTimeOffset.UtcNow;
            // Income 100→200: old effect +100 (oldDelta=-100), new effect +200 (newDelta=+200), total=+100
            await repo.UpdateAsync(transaction, transaction.FinancialAccountId, -100_00, 200_00, CancellationToken.None);
        }

        // Assert
        var balance = await GetAccountBalanceAsync(accountId, workspaceId);
        Assert.Equal(200_00, balance);
    }

    [Fact]
    public async Task UpdateAsync_AccountChanged_ReversesOldAndUpdatesNew()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountAId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 0);
        var accountBId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 0);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await AddTransactionAsync(
            workspaceId, accountAId, categoryId, userId,
            type: TransactionType.Expense, amountMinor: 100_00);

        // Act — move transaction from account A to account B
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new TransactionRepository(db, fixture.Mapper);
            var transaction = await repo.FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
            var oldAccountId = transaction!.FinancialAccountId;
            transaction.FinancialAccountId = accountBId;
            transaction.UpdatedAt = DateTimeOffset.UtcNow;
            // Expense 100: reverse on old (+100), apply on new (-100)
            await repo.UpdateAsync(transaction, oldAccountId, 100_00, -100_00, CancellationToken.None);
        }

        // Assert
        var balanceA = await GetAccountBalanceAsync(accountAId, workspaceId);
        var balanceB = await GetAccountBalanceAsync(accountBId, workspaceId);
        Assert.Equal(0, balanceA);      // restored
        Assert.Equal(-100_00, balanceB); // expense applied
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesTransaction()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);

        // Act
        await DeleteTransactionAsync(transactionId, workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransactionRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_ReversesAccountBalance()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 0);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        await AddTransactionAsync(workspaceId, accountId, categoryId, userId, type: TransactionType.Income, amountMinor: 100_00);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId, type: TransactionType.Income, amountMinor: 100_00);

        // Act
        await DeleteTransactionAsync(transactionId, workspaceId);

        // Assert
        var balance = await GetAccountBalanceAsync(accountId, workspaceId);
        Assert.Equal(100_00, balance);
    }

    private static Transaction NewTransaction(
        Guid workspaceId,
        Guid accountId,
        Guid categoryId,
        Guid createdByUserId,
        string name = "Transaction",
        TransactionType type = TransactionType.Expense,
        long amountMinor = 10_00,
        DateOnly? occurredAt = null) => new()
    {
        Id = Guid.NewGuid(),
        WorkspaceId = workspaceId,
        FinancialAccountId = accountId,
        CategoryId = categoryId,
        CreatedByUserId = createdByUserId,
        Name = name,
        Type = type,
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = occurredAt ?? DateOnly.FromDateTime(DateTime.UtcNow),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private async Task<Guid> AddTransactionAsync(
        Guid workspaceId,
        Guid accountId,
        Guid categoryId,
        Guid createdByUserId,
        string name = "Transaction",
        TransactionType type = TransactionType.Expense,
        long amountMinor = 10_00,
        DateOnly? occurredAt = null)
    {
        var transaction = NewTransaction(workspaceId, accountId, categoryId, createdByUserId, name, type, amountMinor, occurredAt);
        var balanceDelta = type == TransactionType.Income ? amountMinor : -amountMinor;

        await using var db = fixture.CreateDbContext();
        await new TransactionRepository(db, fixture.Mapper)
            .AddAsync(transaction, balanceDelta, CancellationToken.None);

        return transaction.Id;
    }

    private async Task DeleteTransactionAsync(Guid transactionId, Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var repo = new TransactionRepository(db, fixture.Mapper);
        var transaction = await repo.FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
        var balanceDelta = transaction!.Type == TransactionType.Income
            ? -transaction.AmountMinor
            : transaction.AmountMinor;
        await repo.DeleteAsync(transaction, balanceDelta, CancellationToken.None);
    }

    private async Task<long> GetAccountBalanceAsync(Guid accountId, Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var account = await TestData.FindAccountAsync(db, fixture.Mapper, accountId, workspaceId);
        return account!.CurrentBalanceMinor;
    }
}
