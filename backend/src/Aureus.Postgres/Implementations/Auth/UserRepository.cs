using AutoMapper;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Postgres.Entities;
using Aureus.UseCases.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Postgres.Implementations.Auth;

public sealed class UserRepository(AureusDbContext dbContext, IMapper mapper) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var userDb = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        return userDb is null ? null : mapper.Map<User>(userDb);
    }

    public async Task AddAsync(
        User user,
        Workspace workspace,
        WorkspaceMember workspaceMember,
        CancellationToken cancellationToken)
    {
        dbContext.Users.Add(mapper.Map<UserDb>(user));
        dbContext.Workspaces.Add(mapper.Map<WorkspaceDb>(workspace));
        dbContext.WorkspaceMembers.Add(mapper.Map<WorkspaceMemberDb>(workspaceMember));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
