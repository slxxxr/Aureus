using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Categories.CreateCategory;

namespace Aureus.UnitTests.Categories;

public sealed class CreateCategoryHandlerTests
{
    private static CreateCategoryCommand DefaultCommand(
        string name = "Groceries",
        TransactionType type = TransactionType.Expense,
        Guid? workspaceId = null) =>
        new(WorkspaceId: workspaceId ?? Guid.NewGuid(), Name: name, Type: type);

    [Fact]
    public async Task Handle_ValidCommand_SetsWorkspaceAndType()
    {
        // Arrange
        var repository = new CategoryRepositoryMock().CapturingAdd();
        var handler = new CreateCategoryHandler(repository.Object);
        var command = DefaultCommand(type: TransactionType.Income);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.WorkspaceId, result.WorkspaceId);
        Assert.Equal(command.Type, result.Type);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespace()
    {
        // Arrange
        var repository = new CategoryRepositoryMock().CapturingAdd();
        var handler = new CreateCategoryHandler(repository.Object);
        var command = DefaultCommand(name: "  Groceries  ");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Groceries", result.Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_AssignsNonEmptyId()
    {
        // Arrange
        var repository = new CategoryRepositoryMock().CapturingAdd();
        var handler = new CreateCategoryHandler(repository.Object);

        // Act
        var result = await handler.Handle(DefaultCommand(), CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }
}
