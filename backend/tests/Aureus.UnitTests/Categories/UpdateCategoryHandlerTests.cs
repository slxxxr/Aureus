using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Categories.UpdateCategory;

namespace Aureus.UnitTests.Categories;

public sealed class UpdateCategoryHandlerTests
{
    private static Category ExistingCategory(string name = "Groceries") =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            Name = name,
            Type = TransactionType.Expense,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var repository = new CategoryRepositoryMock().WithNoCategory(categoryId, workspaceId);
        var handler = new UpdateCategoryHandler(repository.Object);

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            handler.Handle(
                new UpdateCategoryCommand(categoryId, workspaceId, Name: "New name"),
                CancellationToken.None));

        // Assert
        Assert.Equal(CategoryErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_NameProvided_UpdatesName()
    {
        // Arrange
        var category = ExistingCategory(name: "Old name");
        var repository = new CategoryRepositoryMock()
            .WithCategory(category.Id, category.WorkspaceId, category)
            .CapturingUpdate();
        var handler = new UpdateCategoryHandler(repository.Object);

        // Act
        var command = new UpdateCategoryCommand(category.Id, category.WorkspaceId, Name: "New name");
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.Name, result.Name);
    }

    [Fact]
    public async Task Handle_NameProvided_NormalizesWhitespace()
    {
        // Arrange
        var category = ExistingCategory(name: "Old name");
        var repository = new CategoryRepositoryMock()
            .WithCategory(category.Id, category.WorkspaceId, category)
            .CapturingUpdate();
        var handler = new UpdateCategoryHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, category.WorkspaceId, Name: "  Rent  "),
            CancellationToken.None);

        // Assert
        Assert.Equal("Rent", result.Name);
    }

    [Fact]
    public async Task Handle_NameNull_LeavesNameUnchanged()
    {
        // Arrange
        var category = ExistingCategory(name: "Groceries");
        var repository = new CategoryRepositoryMock()
            .WithCategory(category.Id, category.WorkspaceId, category)
            .CapturingUpdate();
        var handler = new UpdateCategoryHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, category.WorkspaceId, Name: null),
            CancellationToken.None);

        // Assert
        Assert.Equal(category.Name, result.Name);
    }
}
