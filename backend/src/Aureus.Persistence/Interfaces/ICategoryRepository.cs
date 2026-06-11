using Aureus.Domain.Categories;

namespace Aureus.Persistence.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<Category?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken);

    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task UpdateAsync(Category category, CancellationToken cancellationToken);

    Task DeleteAsync(Category category, CancellationToken cancellationToken);
}
