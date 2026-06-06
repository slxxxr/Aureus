using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Categories.DeleteCategory;

namespace Aureus.UnitTests.Categories;

public sealed class DeleteCategoryHandlerTests
{
    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var repository = new CategoryRepositoryMock().WithNoCategory(categoryId, workspaceId);
        var handler = new DeleteCategoryHandler(repository.Object);

        // Act
        var exception = await Assert.ThrowsAsync<CategoryException>(() =>
            handler.Handle(new DeleteCategoryCommand(categoryId, workspaceId), CancellationToken.None));

        // Assert
        Assert.Equal(CategoryErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_DoesNotCallDelete()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var repository = new CategoryRepositoryMock().WithNoCategory(categoryId, workspaceId);
        var handler = new DeleteCategoryHandler(repository.Object);

        // Act
        await Assert.ThrowsAsync<CategoryException>(() =>
            handler.Handle(new DeleteCategoryCommand(categoryId, workspaceId), CancellationToken.None));

        // Assert
        repository.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_CategoryExists_CallsDeleteWithCorrectCategory()
    {
        // Arrange
        var category = new Category
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            Name = "Groceries",
            Type = TransactionType.Expense,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var repository = new CategoryRepositoryMock()
            .WithCategory(category.Id, category.WorkspaceId, category)
            .CapturingDelete();
        var handler = new DeleteCategoryHandler(repository.Object);

        // Act
        await handler.Handle(new DeleteCategoryCommand(category.Id, category.WorkspaceId), CancellationToken.None);

        // Assert
        repository.VerifyDeleteCalledOnce();
        Assert.NotNull(repository.DeletedCategory);
        Assert.Equal(category.Id, repository.DeletedCategory.Id);
    }
}
