using Aureus.Domain.Categories;
using Aureus.Domain.Transactions;
using MediatR;

namespace Aureus.UseCases.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    Guid WorkspaceId,
    string Name,
    TransactionType Type) : IRequest<Category>;
