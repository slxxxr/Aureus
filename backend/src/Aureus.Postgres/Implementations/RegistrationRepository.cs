using AutoMapper;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Persistence;
using Aureus.Persistence.Entities;
using Aureus.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aureus.Postgres.Implementations;

public sealed class RegistrationRepository(AureusDbContext dbContext, IMapper mapper) : IRegistrationRepository
{
    public async Task CreateUserWithWorkspaceAsync(
        User user, Workspace workspace, WorkspaceMember member, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(mapper.Map<UserDb>(user));
        dbContext.Workspaces.Add(mapper.Map<WorkspaceDb>(workspace));
        dbContext.WorkspaceMembers.Add(mapper.Map<WorkspaceMemberDb>(member));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new EmailVerificationException(EmailVerificationErrorCode.EmailAlreadyConfirmed, "Email is already registered.");
        }
    }
}
