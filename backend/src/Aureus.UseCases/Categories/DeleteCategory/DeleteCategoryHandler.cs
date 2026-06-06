using Aureus.Domain.Categories;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await repository.FindByIdAsync(command.CategoryId, command.WorkspaceId, cancellationToken);

        if (category is null)
        {
            throw new CategoryException(
                CategoryErrorCode.NotFound,
                $"Category {command.CategoryId} not found.");
        }

        await repository.DeleteAsync(category, cancellationToken);
    }
}
