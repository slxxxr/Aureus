using Aureus.Domain.Workspaces;
using Aureus.Postgres.Entities;
using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aureus.Postgres.Implementations.Workspaces;

public sealed class WorkspaceRepository(AureusDbContext dbContext, IMapper mapper) : IWorkspaceRepository
{
    public async Task<WorkspaceMembership?> FindMembershipAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var member = await dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .Select(m => new { m.WorkspaceId, m.UserId, m.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            return null;
        }

        return Enum.TryParse<WorkspaceRole>(member.Role, out var role)
            ? new WorkspaceMembership(member.WorkspaceId, member.UserId, role)
            : null;
    }

    public async Task<IReadOnlyList<UserWorkspaceSummary>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => new UserWorkspaceSummary(member.WorkspaceId, member.Workspace.Name, member.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<Workspace?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<Workspace>(entity);
    }

    public async Task AddAsync(Workspace workspace, WorkspaceMember member, CancellationToken cancellationToken)
    {
        dbContext.Workspaces.Add(mapper.Map<WorkspaceDb>(workspace));
        dbContext.WorkspaceMembers.Add(mapper.Map<WorkspaceMemberDb>(member));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new WorkspaceException(WorkspaceErrorCode.NameTaken,
                $"A workspace named '{workspace.Name}' already exists.");
        }
    }

    public async Task UpdateAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Workspaces
                .Where(w => w.Id == workspace.Id)
                .ExecuteUpdateAsync(s => s
                        .SetProperty(w => w.Name, workspace.Name)
                        .SetProperty(w => w.UpdatedAt, workspace.UpdatedAt),
                    cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new WorkspaceException(WorkspaceErrorCode.NameTaken,
                $"A workspace named '{workspace.Name}' already exists.");
        }
    }

    public async Task DeleteAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Transactions
            .Where(t => t.WorkspaceId == workspace.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsDeleted, true)
                .SetProperty(t => t.DeletedAt, now), cancellationToken);

        await dbContext.FinancialAccounts
            .Where(a => a.WorkspaceId == workspace.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.DeletedAt, now), cancellationToken);

        await dbContext.Categories
            .Where(c => c.WorkspaceId == workspace.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.DeletedAt, now), cancellationToken);

        await dbContext.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspace.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsDeleted, true)
                .SetProperty(m => m.DeletedAt, now), cancellationToken);

        await dbContext.Workspaces
            .Where(w => w.Id == workspace.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.IsDeleted, true)
                .SetProperty(w => w.DeletedAt, now), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ||
        ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } };
}
