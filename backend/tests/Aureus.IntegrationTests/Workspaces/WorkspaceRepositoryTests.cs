using Aureus.Domain.Workspaces;
using Aureus.IntegrationTests.Common;
using Aureus.Postgres.Entities;
using Aureus.Postgres.Implementations.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace Aureus.IntegrationTests.Workspaces;

[Collection(nameof(PostgresCollection))]
public sealed class WorkspaceRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidWorkspace_PersistsWorkspaceAndOwnerMember()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var (workspace, member) = NewWorkspace(ownerId, "Personal");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new WorkspaceRepository(db, fixture.Mapper).AddAsync(workspace, member, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var repository = new WorkspaceRepository(assertDb, fixture.Mapper);

        var stored = await repository.FindByIdAsync(workspace.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Personal", stored!.Name);

        var membership = await repository.FindMembershipAsync(workspace.Id, ownerId, CancellationToken.None);
        Assert.NotNull(membership);
        Assert.Equal(WorkspaceRole.Owner, membership!.Role);
    }

    [Fact]
    public async Task AddAsync_DuplicateOwnerAndName_ThrowsNameTaken()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        await AddWorkspaceAsync(ownerId, "Personal");
        var (workspace, member) = NewWorkspace(ownerId, "Personal");

        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceRepository(db, fixture.Mapper);

        // Act
        var exception = await Assert.ThrowsAsync<WorkspaceException>(() =>
            repository.AddAsync(workspace, member, CancellationToken.None));

        // Assert
        Assert.Equal(WorkspaceErrorCode.NameTaken, exception.Code);
    }

    [Fact]
    public async Task AddAsync_SameNameDifferentOwner_Succeeds()
    {
        // Arrange
        var firstOwner = await SeedUserAsync();
        var secondOwner = await SeedUserAsync();
        await AddWorkspaceAsync(firstOwner, "Personal");

        // Act
        var secondId = await AddWorkspaceAsync(secondOwner, "Personal");

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(db, fixture.Mapper).FindByIdAsync(secondId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AddAsync_NameReusedAfterSoftDeletedWorkspace_Succeeds()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var firstId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteWorkspaceAsync(firstId);

        // Act
        var secondId = await AddWorkspaceAsync(ownerId, "Personal");

        // Assert
        await using var db = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(db, fixture.Mapper).FindByIdAsync(secondId, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedWorkspace_ReturnsNull()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteWorkspaceAsync(workspaceId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(db, fixture.Mapper).FindByIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetByUserIdAsync_SoftDeletedWorkspace_ExcludesIt()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var liveId = await AddWorkspaceAsync(ownerId, "Live");
        var deletedId = await AddWorkspaceAsync(ownerId, "Deleted");
        await SoftDeleteWorkspaceAsync(deletedId);

        // Act
        await using var db = fixture.CreateDbContext();
        var summaries = await new WorkspaceRepository(db, fixture.Mapper).GetByUserIdAsync(ownerId, CancellationToken.None);

        // Assert
        Assert.Single(summaries);
        Assert.Equal(liveId, summaries[0].Id);
    }

    [Fact]
    public async Task FindMembershipAsync_SoftDeletedMember_ReturnsNull()
    {
        // Arrange
        var ownerId = await SeedUserAsync();
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteMemberAsync(workspaceId, ownerId);

        // Act
        await using var db = fixture.CreateDbContext();
        var membership = await new WorkspaceRepository(db, fixture.Mapper)
            .FindMembershipAsync(workspaceId, ownerId, CancellationToken.None);

        // Assert
        Assert.Null(membership);
    }

    private static (Workspace Workspace, WorkspaceMember Member) NewWorkspace(Guid ownerId, string name)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            JoinedAt = workspace.CreatedAt,
        };

        return (workspace, member);
    }

    private async Task<Guid> SeedUserAsync()
    {
        await using var db = fixture.CreateDbContext();
        var user = new UserDb
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private async Task<Guid> AddWorkspaceAsync(Guid ownerId, string name)
    {
        var (workspace, member) = NewWorkspace(ownerId, name);

        await using var db = fixture.CreateDbContext();
        await new WorkspaceRepository(db, fixture.Mapper).AddAsync(workspace, member, CancellationToken.None);

        return workspace.Id;
    }

    private async Task SoftDeleteWorkspaceAsync(Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        await db.Workspaces
            .Where(w => w.Id == workspaceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.IsDeleted, true)
                .SetProperty(w => w.DeletedAt, DateTimeOffset.UtcNow));
    }

    private async Task SoftDeleteMemberAsync(Guid workspaceId, Guid userId)
    {
        await using var db = fixture.CreateDbContext();
        await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsDeleted, true)
                .SetProperty(m => m.DeletedAt, DateTimeOffset.UtcNow));
    }
}
