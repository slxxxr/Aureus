using Aureus.Domain.Categories;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class CategoryRepositoryMock
{
    private readonly Mock<ICategoryRepository> _mock = new();

    public ICategoryRepository Object => _mock.Object;

    public Category? SavedCategory { get; private set; }

    public Category? UpdatedCategory { get; private set; }

    public Category? DeletedCategory { get; private set; }

    public CategoryRepositoryMock WithCategory(Guid id, Guid workspaceId, Category category)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        return this;
    }

    public CategoryRepositoryMock WithNoCategory(Guid id, Guid workspaceId)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        return this;
    }

    public CategoryRepositoryMock CapturingAdd()
    {
        _mock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => SavedCategory = category)
            .Returns(Task.CompletedTask);

        return this;
    }

    public CategoryRepositoryMock CapturingUpdate()
    {
        _mock
            .Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => UpdatedCategory = category)
            .Returns(Task.CompletedTask);

        return this;
    }

    public CategoryRepositoryMock CapturingDelete()
    {
        _mock
            .Setup(r => r.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => DeletedCategory = category)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyDeleteCalledOnce() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteNotCalled() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
}
