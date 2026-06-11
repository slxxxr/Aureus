using Aureus.Postgres.Implementations;
using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using Aureus.IntegrationTests.Common;


namespace Aureus.IntegrationTests.Categories;

[Collection(nameof(PostgresCollection))]
public sealed class CategoryRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidCategory_PersistsCategory()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (firstWorkspace, _) = await TestData.SeedWorkspaceAsync(fixture);
        var (secondWorkspace, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var (otherWorkspace, _) = await TestData.SeedWorkspaceAsync(fixture);
        var categoryId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new CategoryRepository(db, fixture.Mapper)
            .FindByIdAsync(categoryId, otherWorkspace, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task UpdateAsync_ValidCategory_UpdatesName()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var categoryId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new CategoryRepository(db, fixture.Mapper);
            var category = await repo.FindByIdAsync(categoryId, workspaceId, CancellationToken.None);
            category!.Name = "Food";
            category.UpdatedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(category, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new CategoryRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(categoryId, workspaceId, CancellationToken.None);
        Assert.Equal("Food", stored!.Name);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsNameTaken()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);
        var secondId = await AddCategoryAsync(workspaceId, "Transport", TransactionType.Expense);

        await using var db = fixture.CreateDbContext();
        var repo = new CategoryRepository(db, fixture.Mapper);
        var category = await repo.FindByIdAsync(secondId, workspaceId, CancellationToken.None);
        category!.Name = "Groceries";

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            repo.UpdateAsync(category, CancellationToken.None));

        // Assert
        Assert.Equal(CategoryErrorCode.NameTaken, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotDeleteTransactions()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await AddCategoryAsync(workspaceId, "Groceries", TransactionType.Expense);
        var transactionId = await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryId, ownerId);

        // Act
        await DeleteCategoryAsync(workspaceId, categoryId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var transaction = await TestData.FindTransactionAsync(assertDb, fixture.Mapper, transactionId, workspaceId);
        Assert.NotNull(transaction);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_SoftDeletedCategory_ExcludesIt()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
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
