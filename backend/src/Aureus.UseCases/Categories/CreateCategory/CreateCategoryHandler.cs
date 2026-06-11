using Aureus.Domain.Categories;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.Categories.CreateCategory;

public sealed class CreateCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<CreateCategoryCommand, Category>
{
    public async Task<Category> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            WorkspaceId = command.WorkspaceId,
            Name = command.Name.Trim(),
            Type = command.Type,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await repository.AddAsync(category, cancellationToken);

        return category;
    }
}
