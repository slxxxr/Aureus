using Aureus.Domain.Workspaces;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations;

public sealed class WorkspaceInvitationRepository(AureusDbContext dbContext, IMapper mapper)
    : IWorkspaceInvitationRepository
{
    public async Task<WorkspaceInvitation?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.WorkspaceInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<WorkspaceInvitation>(entity);
    }

    public async Task<WorkspaceInvitation?> FindPendingAsync(
        Guid workspaceId, string email, CancellationToken cancellationToken)
    {
        var entity = await dbContext.WorkspaceInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Email == email, cancellationToken);

        return entity is null ? null : mapper.Map<WorkspaceInvitation>(entity);
    }

    public async Task<IReadOnlyList<WorkspaceInvitation>> GetPendingForWorkspaceAsync(
        Guid workspaceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var entities = await dbContext.WorkspaceInvitations
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId && i.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<WorkspaceInvitation>>(entities);
    }

    public async Task<IReadOnlyList<PendingInvitationSummary>> GetPendingForEmailAsync(
        string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await dbContext.WorkspaceInvitations
            .AsNoTracking()
            .Where(i => i.Email == email && i.ExpiresAt > now)
            .Join(dbContext.Workspaces, i => i.WorkspaceId, w => w.Id,
                (i, w) => new PendingInvitationSummary(i.Id, i.WorkspaceId, w.Name, i.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(Guid workspaceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId && i.ExpiresAt > now)
            .CountAsync(cancellationToken);
    }

    public async Task UpsertAsync(WorkspaceInvitation invitation, CancellationToken cancellationToken)
    {
        var existing = await dbContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.WorkspaceId == invitation.WorkspaceId && i.Email == invitation.Email, cancellationToken);

        if (existing is null)
        {
            dbContext.WorkspaceInvitations.Add(mapper.Map<WorkspaceInvitationDb>(invitation));
        }
        else
        {
            existing.ExpiresAt = invitation.ExpiresAt;
            existing.InvitedByUserId = invitation.InvitedByUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.WorkspaceInvitations
            .Where(i => i.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AcceptAsync(Guid invitationId, WorkspaceMember newMember, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.WorkspaceInvitations
            .Where(i => i.Id == invitationId)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.WorkspaceMembers.Add(mapper.Map<WorkspaceMemberDb>(newMember));
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
