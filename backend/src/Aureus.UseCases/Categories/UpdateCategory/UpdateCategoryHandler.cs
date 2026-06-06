using Aureus.Domain.Categories;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<UpdateCategoryCommand, Category>
{
    public async Task<Category> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await repository.FindByIdAsync(command.CategoryId, command.WorkspaceId, cancellationToken);

        if (category is null)
        {
            throw new CategoryException(
                CategoryErrorCode.NotFound,
                $"Category {command.CategoryId} not found.");
        }

        if (command.Name is not null)
        {
            category.Name = command.Name.Trim();
        }

        category.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(category, cancellationToken);

        return category;
    }
}
