using MediatR;

namespace Aureus.UseCases.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId, Guid WorkspaceId) : IRequest;
