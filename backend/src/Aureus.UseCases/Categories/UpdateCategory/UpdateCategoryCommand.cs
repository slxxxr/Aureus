using Aureus.Domain.Categories;
using MediatR;

namespace Aureus.UseCases.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    Guid WorkspaceId,
    string? Name) : IRequest<Category>;
