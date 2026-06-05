using AutoMapper;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Infrastructure.Persistence.Entities;
using Aureus.UseCases.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aureus.Infrastructure.Persistence;

public sealed class UserRegistrationDb(AureusDbContext dbContext, IMapper mapper) : IUserRegistrationDb
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
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
