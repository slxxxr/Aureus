using Aureus.Domain.Transactions;
using Aureus.IntegrationTests.Common;
using Aureus.Postgres.Entities;
using Aureus.Postgres.Implementations.Categories;
using Aureus.Postgres.Implementations.FinancialAccounts;
using Aureus.Postgres.Implementations.Transactions;

namespace Aureus.IntegrationTests.Transactions;

[Collection(nameof(PostgresCollection))]
public sealed class TransactionRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidTransaction_PersistsTransaction()
    {
        // Arrange
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
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
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId, initialBalance: 100_00);
        var categoryId = await SeedCategoryAsync(workspaceId);
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
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);
        await DeleteTransactionAsync(transactionId, workspaceId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new TransactionRepository(db, fixture.Mapper)
            .FindByIdAsync(transactionId, workspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task FindByIdAsync_WrongWorkspace_ReturnsNull()
    {
        // Arrange
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var (otherWorkspaceId, _) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
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
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
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
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
        var now = DateTimeOffset.UtcNow;
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
    public async Task UpdateAsync_ValidTransaction_UpdatesFields()
    {
        // Arrange
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
        var newCategoryId = await SeedCategoryAsync(workspaceId);
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
            await repo.UpdateAsync(transaction, 0, CancellationToken.None);
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
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId, initialBalance: 0);
        var categoryId = await SeedCategoryAsync(workspaceId);
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
            await repo.UpdateAsync(transaction, 100_00, CancellationToken.None);
        }

        // Assert
        var balance = await GetAccountBalanceAsync(accountId, workspaceId);
        Assert.Equal(200_00, balance);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesTransaction()
    {
        // Arrange
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
        var transactionId = await AddTransactionAsync(workspaceId, accountId, categoryId, userId);
        await DeleteTransactionAsync(transactionId, workspaceId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new TransactionRepository(db, fixture.Mapper)
            .FindByIdAsync(transactionId, workspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_ReversesAccountBalance()
    {
        // Arrange
        var (workspaceId, userId) = await SeedWorkspaceAsync();
        var accountId = await SeedAccountAsync(workspaceId, initialBalance: 0);
        var categoryId = await SeedCategoryAsync(workspaceId);
        await AddTransactionAsync(
            workspaceId, accountId, categoryId, userId,
            type: TransactionType.Income, amountMinor: 100_00);
        var transactionId = await AddTransactionAsync(
            workspaceId, accountId, categoryId, userId,
            type: TransactionType.Income, amountMinor: 100_00);
        await DeleteTransactionAsync(transactionId, workspaceId);

        // Act & Assert
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
        DateTimeOffset? occurredAt = null) => new()
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
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private async Task<Guid> SeedUserAsync()
    {
        await using var db = fixture.CreateDbContext();
        var user = new UserDb
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<(Guid WorkspaceId, Guid OwnerId)> SeedWorkspaceAsync()
    {
        var ownerId = await SeedUserAsync();
        await using var db = fixture.CreateDbContext();
        var workspace = new WorkspaceDb
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerId,
            Name = $"ws-{Guid.NewGuid():N}",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        return (workspace.Id, ownerId);
    }

    private async Task<Guid> SeedAccountAsync(Guid workspaceId, long initialBalance = 0)
    {
        await using var db = fixture.CreateDbContext();
        var account = new Domain.FinancialAccounts.FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = $"account-{Guid.NewGuid():N}",
            Currency = "RUB",
            InitialBalanceMinor = initialBalance,
            CurrentBalanceMinor = initialBalance,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await new FinancialAccountRepository(db, fixture.Mapper).AddAsync(account, CancellationToken.None);
        return account.Id;
    }

    private async Task<Guid> SeedCategoryAsync(Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var category = new Domain.Categories.Category
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = $"cat-{Guid.NewGuid():N}",
            Type = TransactionType.Expense,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await new CategoryRepository(db, fixture.Mapper).AddAsync(category, CancellationToken.None);
        return category.Id;
    }

    private async Task<Guid> AddTransactionAsync(
        Guid workspaceId,
        Guid accountId,
        Guid categoryId,
        Guid createdByUserId,
        string name = "Transaction",
        TransactionType type = TransactionType.Expense,
        long amountMinor = 10_00,
        DateTimeOffset? occurredAt = null)
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
        var account = await new FinancialAccountRepository(db, fixture.Mapper)
            .FindByIdAsync(accountId, workspaceId, CancellationToken.None);
        return account!.CurrentBalanceMinor;
    }
}
