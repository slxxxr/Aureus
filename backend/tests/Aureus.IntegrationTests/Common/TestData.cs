using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Postgres;
using Aureus.Postgres.Entities;
using Aureus.Postgres.Implementations.Categories;
using Aureus.Postgres.Implementations.FinancialAccounts;
using Aureus.Postgres.Implementations.Transactions;
using AutoMapper;

namespace Aureus.IntegrationTests.Common;

public static class TestData
{
    public static async Task<Guid> SeedUserAsync(PostgresFixture fixture)
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

    public static async Task<(Guid WorkspaceId, Guid OwnerId)> SeedWorkspaceAsync(PostgresFixture fixture)
    {
        var ownerId = await SeedUserAsync(fixture);
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

    public static async Task<Guid> SeedAccountAsync(PostgresFixture fixture, Guid workspaceId, long initialBalance = 0)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = $"account-{Guid.NewGuid():N}",
            Currency = "RUB",
            InitialBalanceMinor = initialBalance,
            CurrentBalanceMinor = initialBalance,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await using var db = fixture.CreateDbContext();
        await new FinancialAccountRepository(db, fixture.Mapper).AddAsync(account, CancellationToken.None);
        return account.Id;
    }

    public static async Task<Guid> SeedCategoryAsync(PostgresFixture fixture, Guid workspaceId)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = $"cat-{Guid.NewGuid():N}",
            Type = TransactionType.Expense,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await using var db = fixture.CreateDbContext();
        await new CategoryRepository(db, fixture.Mapper).AddAsync(category, CancellationToken.None);
        return category.Id;
    }

    public static async Task<Guid> SeedTransactionAsync(
        PostgresFixture fixture,
        Guid workspaceId,
        Guid accountId,
        Guid categoryId,
        Guid createdByUserId)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            FinancialAccountId = accountId,
            CategoryId = categoryId,
            CreatedByUserId = createdByUserId,
            Name = "Transaction",
            Type = TransactionType.Expense,
            AmountMinor = 10_00,
            Currency = "RUB",
            OccurredAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await using var db = fixture.CreateDbContext();
        await new TransactionRepository(db, fixture.Mapper).AddAsync(transaction, -10_00, CancellationToken.None);
        return transaction.Id;
    }

    public static async Task<FinancialAccount?> FindAccountAsync(
        AureusDbContext db, IMapper mapper, Guid accountId, Guid workspaceId) =>
        await new FinancialAccountRepository(db, mapper).FindByIdAsync(accountId, workspaceId, CancellationToken.None);

    public static async Task<Category?> FindCategoryAsync(
        AureusDbContext db, IMapper mapper, Guid categoryId, Guid workspaceId) =>
        await new CategoryRepository(db, mapper).FindByIdAsync(categoryId, workspaceId, CancellationToken.None);

    public static async Task<Transaction?> FindTransactionAsync(
        AureusDbContext db, IMapper mapper, Guid transactionId, Guid workspaceId) =>
        await new TransactionRepository(db, mapper).FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
}
