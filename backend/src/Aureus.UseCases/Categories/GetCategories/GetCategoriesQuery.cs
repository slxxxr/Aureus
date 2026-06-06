using Aureus.Domain.Categories;
using MediatR;

namespace Aureus.UseCases.Categories.GetCategories;

public sealed record GetCategoriesQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<Category>>;
