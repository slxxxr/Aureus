using Aureus.Domain.Categories;
using Aureus.UseCases.Common.Persistence;
using MediatR;

namespace Aureus.UseCases.Categories.GetCategories;

public sealed class GetCategoriesHandler(ICategoryRepository repository)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
    public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        return repository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);
    }
}
