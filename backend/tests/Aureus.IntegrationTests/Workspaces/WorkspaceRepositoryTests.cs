using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Domain.Workspaces;
using Aureus.IntegrationTests.Common;
using Aureus.Postgres.Entities;
using Aureus.Postgres.Implementations.Categories;
using Aureus.Postgres.Implementations.FinancialAccounts;
using Aureus.Postgres.Implementations.Transactions;
using Aureus.Postgres.Implementations.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace Aureus.IntegrationTests.Workspaces;

[Collection(nameof(PostgresCollection))]
public sealed class WorkspaceRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidWorkspace_PersistsWorkspaceAndOwnerMember()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var (workspace, member) = NewWorkspace(ownerId, "Personal");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new WorkspaceRepository(db, fixture.Mapper).AddAsync(workspace, member, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var repository = new WorkspaceRepository(assertDb, fixture.Mapper);

        var stored = await repository.FindByIdAsync(workspace.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(workspace.Name, stored!.Name);

        var membership = await repository.FindMembershipAsync(workspace.Id, ownerId, CancellationToken.None);
        Assert.NotNull(membership);
        Assert.Equal(member.Role, membership!.Role);
    }

    [Fact]
    public async Task AddAsync_DuplicateOwnerAndName_ThrowsNameTaken()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        await AddWorkspaceAsync(ownerId, "Personal");
        var (workspace, member) = NewWorkspace(ownerId, "Personal");

        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceRepository(db, fixture.Mapper);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceException>(() =>
            repository.AddAsync(workspace, member, CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceErrorCode.NameTaken, exception.Code);
    }

    [Fact]
    public async Task AddAsync_SameNameDifferentOwner_Succeeds()
    {
        // Arrange
        var firstOwner = await SeedUserAsync();
        var secondOwner = await SeedUserAsync();
        await AddWorkspaceAsync(firstOwner, "Personal");

        // Act
        var secondId = await AddWorkspaceAsync(secondOwner, "Personal");

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(db, fixture.Mapper).FindByIdAsync(secondId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AddAsync_NameReusedAfterSoftDeletedWorkspace_Succeeds()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var firstId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteWorkspaceAsync(firstId);

        // Act
        var secondId = await AddWorkspaceAsync(ownerId, "Personal");

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(db, fixture.Mapper).FindByIdAsync(secondId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedWorkspace_ReturnsNull()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteWorkspaceAsync(workspaceId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(db, fixture.Mapper).FindByIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetByUserIdAsync_SoftDeletedWorkspace_ExcludesIt()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var liveId = await AddWorkspaceAsync(ownerId, "Live");
        var deletedId = await AddWorkspaceAsync(ownerId, "Deleted");
        await SoftDeleteWorkspaceAsync(deletedId);

        // Act
        await using var db = fixture.CreateDbContext();
        var summaries = await new WorkspaceRepository(db, fixture.Mapper).GetByUserIdAsync(ownerId, CancellationToken.None);

        // Assert
        Assert.Single(summaries);
        Assert.Equal(liveId, summaries[0].Id);
    }

    [Fact]
    public async Task FindMembershipAsync_SoftDeletedMember_ReturnsNull()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteMemberAsync(workspaceId, ownerId);

        // Act
        await using var db = fixture.CreateDbContext();
        var membership = await new WorkspaceRepository(db, fixture.Mapper)
            .FindMembershipAsync(workspaceId, ownerId, CancellationToken.None);

        // Assert
        Assert.Null(membership);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesWorkspace()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new WorkspaceRepository(db, fixture.Mapper);
            var workspace = await repo.FindByIdAsync(workspaceId, CancellationToken.None);
            await repo.DeleteAsync(workspace!, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToWorkspaceMembers()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var membership = await new WorkspaceRepository(assertDb, fixture.Mapper)
            .FindMembershipAsync(workspaceId, ownerId, CancellationToken.None);
        Assert.Null(membership);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToFinancialAccounts()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var accountId = await SeedAccountAsync(workspaceId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var account = await new FinancialAccountRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(accountId, workspaceId, CancellationToken.None);
        Assert.Null(account);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToCategories()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var categoryId = await SeedCategoryAsync(workspaceId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var category = await new CategoryRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(categoryId, workspaceId, CancellationToken.None);
        Assert.Null(category);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToTransactions()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var accountId = await SeedAccountAsync(workspaceId);
        var categoryId = await SeedCategoryAsync(workspaceId);
        var transactionId = await SeedTransactionAsync(workspaceId, accountId, categoryId, ownerId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var transaction = await new TransactionRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transactionId, workspaceId, CancellationToken.None);
        Assert.Null(transaction);
    }

    private static (Workspace Workspace, WorkspaceMember Member) NewWorkspace(Guid ownerId, string name)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            JoinedAt = workspace.CreatedAt,
        };

        return (workspace, member);
    }

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

    private async Task<Guid> AddWorkspaceAsync(Guid ownerId, string name)
    {
        var (workspace, member) = NewWorkspace(ownerId, name);

        await using var db = fixture.CreateDbContext();
        await new WorkspaceRepository(db, fixture.Mapper).AddAsync(workspace, member, CancellationToken.None);

        return workspace.Id;
    }

    private async Task SoftDeleteWorkspaceAsync(Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        await db.Workspaces
            .Where(w => w.Id == workspaceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.IsDeleted, true)
                .SetProperty(w => w.DeletedAt, DateTimeOffset.UtcNow));
    }

    private async Task SoftDeleteMemberAsync(Guid workspaceId, Guid userId)
    {
        await using var db = fixture.CreateDbContext();
        await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsDeleted, true)
                .SetProperty(m => m.DeletedAt, DateTimeOffset.UtcNow));
    }

    private async Task DeleteWorkspaceAsync(Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var repo = new WorkspaceRepository(db, fixture.Mapper);
        var workspace = await repo.FindByIdAsync(workspaceId, CancellationToken.None);
        await repo.DeleteAsync(workspace!, CancellationToken.None);
    }

    private async Task<Guid> SeedAccountAsync(Guid workspaceId)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = $"account-{Guid.NewGuid():N}",
            Currency = "RUB",
            InitialBalanceMinor = 0,
            CurrentBalanceMinor = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var db = fixture.CreateDbContext();
        await new FinancialAccountRepository(db, fixture.Mapper).AddAsync(account, CancellationToken.None);

        return account.Id;
    }

    private async Task<Guid> SeedCategoryAsync(Guid workspaceId)
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

    private async Task<Guid> SeedTransactionAsync(
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
}
