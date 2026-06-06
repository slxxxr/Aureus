using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using Aureus.IntegrationTests.Common;
using Aureus.Postgres.Entities;
using Aureus.Postgres.Implementations.Categories;
using Microsoft.EntityFrameworkCore;

namespace Aureus.IntegrationTests.Categories;

[Collection(nameof(PostgresCollection))]
public sealed class CategoryRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidCategory_PersistsCategory()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        var category = NewCategory(workspaceId, "Groceries", TransactionType.Expense);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new CategoryRepository(db, fixture.Mapper).AddAsync(category, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new CategoryRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(category.Id, workspaceId, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(category.Name, stored!.Name);
        Assert.Equal(category.Type, stored.Type);
    }

    [Fact]
    public async Task AddAsync_DuplicateWorkspaceNameAndType_ThrowsNameTaken()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);
        var duplicate = NewCategory(workspaceId, "Groceries", TransactionType.Expense);

        await using var db = fixture.CreateDbContext();
        var repository = new CategoryRepository(db, fixture.Mapper);

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            repository.AddAsync(duplicate, CancellationToken.None));

        // Assert
        Assert.Equal(CategoryErrorCode.NameTaken, exception.Code);
    }

    [Fact]
    public async Task AddAsync_SameNameDifferentType_Succeeds()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        await AddCategoryAsync(workspaceId, "Bonus", TransactionType.Expense);

        // Act
        var incomeId = await AddCategoryAsync(workspaceId, "Bonus", TransactionType.Income);

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new CategoryRepository(db, fixture.Mapper)
            .FindByIdAsync(incomeId, workspaceId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AddAsync_SameNameAndTypeDifferentWorkspace_Succeeds()
    {
        // Arrange
        var firstWorkspace = await SeedWorkspaceAsync();
        var secondWorkspace = await SeedWorkspaceAsync();
        await AddCategoryAsync(firstWorkspace, "Groceries", TransactionType.Expense);

        // Act
        var secondId = await AddCategoryAsync(secondWorkspace, "Groceries", TransactionType.Expense);

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new CategoryRepository(db, fixture.Mapper)
            .FindByIdAsync(secondId, secondWorkspace, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AddAsync_NameReusedAfterSoftDeletedCategory_Succeeds()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        var firstId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);
        await DeleteCategoryAsync(workspaceId, firstId);

        // Act
        var secondId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new CategoryRepository(db, fixture.Mapper)
            .FindByIdAsync(secondId, workspaceId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedCategory_ReturnsNull()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        var categoryId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);
        await DeleteCategoryAsync(workspaceId, categoryId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new CategoryRepository(db, fixture.Mapper)
            .FindByIdAsync(categoryId, workspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task FindByIdAsync_WrongWorkspace_ReturnsNull()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        var otherWorkspace = await SeedWorkspaceAsync();
        var categoryId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new CategoryRepository(db, fixture.Mapper)
            .FindByIdAsync(categoryId, otherWorkspace, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_SoftDeletedCategory_ExcludesIt()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        var liveId = await AddCategoryAsync(workspaceId, "Live", TransactionType.Expense);
        var deletedId = await AddCategoryAsync(workspaceId, "Deleted", TransactionType.Expense);
        await DeleteCategoryAsync(workspaceId, deletedId);

        // Act
        await using var db = fixture.CreateDbContext();
        var categories = await new CategoryRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Single(categories);
        Assert.Equal(liveId, categories[0].Id);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_ReturnsCategoriesOrderedByCreatedAt()
    {
        // Arrange
        var workspaceId = await SeedWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;
        var newerId = await AddCategoryAsync(workspaceId, "Newer", TransactionType.Expense, now);
        var olderId = await AddCategoryAsync(workspaceId, "Older", TransactionType.Expense, now.AddDays(-2));

        // Act
        await using var db = fixture.CreateDbContext();
        var categories = await new CategoryRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Equal([olderId, newerId], categories.Select(category => category.Id));
    }

    private static Category NewCategory(
        Guid workspaceId,
        string name,
        TransactionType type,
        DateTimeOffset? createdAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Type = type,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
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

    private async Task<Guid> SeedWorkspaceAsync()
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

        return workspace.Id;
    }

    private async Task<Guid> AddCategoryAsync(
        Guid workspaceId,
        string name,
        TransactionType type,
        DateTimeOffset? createdAt = null)
    {
        var category = NewCategory(workspaceId, name, type, createdAt);

        await using var db = fixture.CreateDbContext();
        await new CategoryRepository(db, fixture.Mapper).AddAsync(category, CancellationToken.None);

        return category.Id;
    }

    private async Task DeleteCategoryAsync(Guid workspaceId, Guid categoryId)
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CategoryRepository(db, fixture.Mapper);

        var category = await repository.FindByIdAsync(categoryId, workspaceId, CancellationToken.None);
        await repository.DeleteAsync(category!, CancellationToken.None);
    }
}
