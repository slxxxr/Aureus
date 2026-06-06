using Aureus.Domain.Categories;
using Aureus.Postgres.Entities;
using Aureus.UseCases.Common.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aureus.Postgres.Implementations.Categories;

public sealed class CategoryRepository(AureusDbContext dbContext, IMapper mapper) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.WorkspaceId == workspaceId)
            .OrderBy(category => category.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<Category>>(entities);
    }

    public async Task<Category?> FindByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id && category.WorkspaceId == workspaceId, cancellationToken);

        return entity is null ? null : mapper.Map<Category>(entity);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<CategoryDb>(category);
        dbContext.Categories.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new CategoryException(
                CategoryErrorCode.NameTaken,
                $"A {category.Type} category named '{category.Name}' already exists in this workspace.");
        }
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Categories
                .Where(entity => entity.Id == category.Id)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(entity => entity.Name, category.Name)
                        .SetProperty(entity => entity.UpdatedAt, category.UpdatedAt),
                    cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new CategoryException(
                CategoryErrorCode.NameTaken,
                $"A {category.Type} category named '{category.Name}' already exists in this workspace.");
        }
    }

    public async Task DeleteAsync(Category category, CancellationToken cancellationToken)
    {
        await dbContext.Categories
            .Where(entity => entity.Id == category.Id)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(entity => entity.IsDeleted, true)
                    .SetProperty(entity => entity.DeletedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private static bool IsUniqueViolation(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ||
        ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } };
}
