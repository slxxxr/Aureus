using Aureus.Postgres.Implementations;
using Aureus.Domain.FinancialAccounts;
using Aureus.IntegrationTests.Common;

using Microsoft.EntityFrameworkCore;

namespace Aureus.IntegrationTests.FinancialAccounts;

[Collection(nameof(PostgresCollection))]
public sealed class FinancialAccountRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidAccount_PersistsAccount()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var account = NewAccount(workspaceId, "Cash");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new FinancialAccountRepository(db, fixture.Mapper).AddAsync(account, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(account.Id, workspaceId, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(account.Name, stored!.Name);
        Assert.Equal(account.Currency, stored.Currency);
        Assert.Equal(account.CurrentBalanceMinor, stored.CurrentBalanceMinor);
    }

    [Fact]
    public async Task AddAsync_DuplicateWorkspaceAndName_ThrowsNameTaken()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await AddAccountAsync(workspaceId, "Cash");
        var duplicate = NewAccount(workspaceId, "Cash");

        await using var db = fixture.CreateDbContext();
        var repository = new FinancialAccountRepository(db, fixture.Mapper);

        // Act
        var exception = await Assert.ThrowsAsync<FinancialAccountException>(() =>
            repository.AddAsync(duplicate, CancellationToken.None));

        // Assert
        Assert.Equal(FinancialAccountErrorCode.NameTaken, exception.Code);
    }

    [Fact]
    public async Task AddAsync_SameNameDifferentWorkspace_Succeeds()
    {
        // Arrange
        var (firstWorkspace, _) = await TestData.SeedWorkspaceAsync(fixture);
        var (secondWorkspace, _) = await TestData.SeedWorkspaceAsync(fixture);
        await AddAccountAsync(firstWorkspace, "Cash");

        // Act
        var secondId = await AddAccountAsync(secondWorkspace, "Cash");

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(db, fixture.Mapper)
            .FindByIdAsync(secondId, secondWorkspace, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AddAsync_NameReusedAfterSoftDeletedAccount_Succeeds()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var firstId = await AddAccountAsync(workspaceId, "Cash");
        await SoftDeleteAccountAsync(firstId);

        // Act
        var secondId = await AddAccountAsync(workspaceId, "Cash");

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(db, fixture.Mapper)
            .FindByIdAsync(secondId, workspaceId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedAccount_ReturnsNull()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await AddAccountAsync(workspaceId, "Cash");
        await SoftDeleteAccountAsync(accountId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(db, fixture.Mapper)
            .FindByIdAsync(accountId, workspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task FindByIdAsync_WrongWorkspace_ReturnsNull()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var (otherWorkspace, _) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await AddAccountAsync(workspaceId, "Cash");

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(db, fixture.Mapper)
            .FindByIdAsync(accountId, otherWorkspace, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task UpdateAsync_ValidAccount_UpdatesFields()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await AddAccountAsync(workspaceId, "Cash");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new FinancialAccountRepository(db, fixture.Mapper);
            var account = await repo.FindByIdAsync(accountId, workspaceId, CancellationToken.None);
            account!.Name = "Wallet";
            account.InitialBalanceMinor = 500_00;
            account.CurrentBalanceMinor = 500_00;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(account, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(accountId, workspaceId, CancellationToken.None);
        Assert.Equal("Wallet", stored!.Name);
        Assert.Equal(500_00, stored.InitialBalanceMinor);
        Assert.Equal(500_00, stored.CurrentBalanceMinor);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsNameTaken()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await AddAccountAsync(workspaceId, "Cash");
        var secondId = await AddAccountAsync(workspaceId, "Card");

        await using var db = fixture.CreateDbContext();
        var repo = new FinancialAccountRepository(db, fixture.Mapper);
        var account = await repo.FindByIdAsync(secondId, workspaceId, CancellationToken.None);
        account!.Name = "Cash";

        // Act
        var exception = await Assert.ThrowsAsync<FinancialAccountException>(() =>
            repo.UpdateAsync(account, CancellationToken.None));

        // Assert
        Assert.Equal(FinancialAccountErrorCode.NameTaken, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAccount()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await AddAccountAsync(workspaceId, "Cash");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new FinancialAccountRepository(db, fixture.Mapper);
            var account = await repo.FindByIdAsync(accountId, workspaceId, CancellationToken.None);
            await repo.DeleteAsync(account!, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new FinancialAccountRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(accountId, workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToTransactions()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await AddAccountAsync(workspaceId, "Cash");
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryId, ownerId);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new FinancialAccountRepository(db, fixture.Mapper);
            var account = await repo.FindByIdAsync(accountId, workspaceId, CancellationToken.None);
            await repo.DeleteAsync(account!, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var transaction = await TestData.FindTransactionAsync(assertDb, fixture.Mapper, transactionId, workspaceId);
        Assert.Null(transaction);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_SoftDeletedAccount_ExcludesIt()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var liveId = await AddAccountAsync(workspaceId, "Live");
        var deletedId = await AddAccountAsync(workspaceId, "Deleted");
        await SoftDeleteAccountAsync(deletedId);

        // Act
        await using var db = fixture.CreateDbContext();
        var accounts = await new FinancialAccountRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Single(accounts);
        Assert.Equal(liveId, accounts[0].Id);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_ReturnsAccountsOrderedByCreatedAt()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var now = DateTimeOffset.UtcNow;
        var newerId = await AddAccountAsync(workspaceId, "Newer", now);
        var olderId = await AddAccountAsync(workspaceId, "Older", now.AddDays(-2));

        // Act
        await using var db = fixture.CreateDbContext();
        var accounts = await new FinancialAccountRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Equal([olderId, newerId], accounts.Select(account => account.Id));
    }

    private static FinancialAccount NewAccount(Guid workspaceId, string name, DateTimeOffset? createdAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Currency = "RUB",
            InitialBalanceMinor = 1000_00,
            CurrentBalanceMinor = 1000_00,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };

    private async Task<Guid> AddAccountAsync(Guid workspaceId, string name, DateTimeOffset? createdAt = null)
    {
        var account = NewAccount(workspaceId, name, createdAt);

        await using var db = fixture.CreateDbContext();
        await new FinancialAccountRepository(db, fixture.Mapper).AddAsync(account, CancellationToken.None);

        return account.Id;
    }

    private async Task SoftDeleteAccountAsync(Guid accountId)
    {
        await using var db = fixture.CreateDbContext();
        await db.FinancialAccounts
            .Where(account => account.Id == accountId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(account => account.IsDeleted, true)
                .SetProperty(account => account.DeletedAt, DateTimeOffset.UtcNow));
    }
}
