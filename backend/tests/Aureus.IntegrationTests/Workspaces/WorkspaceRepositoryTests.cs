using Aureus.Postgres.Implementations;
using Aureus.Domain.Workspaces;
using Aureus.IntegrationTests.Common;
using Aureus.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

namespace Aureus.IntegrationTests.Workspaces;

[Collection(nameof(PostgresCollection))]
public sealed class WorkspaceRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidWorkspace_PersistsWorkspaceAndOwnerMember()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
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
        Assert.Equal(workspace.Name, stored!.Name);

        var membership = await repository.FindMembershipAsync(workspace.Id, ownerId, CancellationToken.None);
        Assert.NotNull(membership);
        Assert.Equal(member.Role, membership!.Role);
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedWorkspace_ReturnsNull()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
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
        var ownerId = await TestData.SeedUserAsync(fixture);
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
    public async Task UpdateAsync_ValidWorkspace_UpdatesName()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new WorkspaceRepository(db, fixture.Mapper);
            var workspace = await repo.FindByIdAsync(workspaceId, CancellationToken.None);
            workspace!.Name = "Business";
            workspace.UpdatedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(workspace, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(workspaceId, CancellationToken.None);
        Assert.Equal("Business", stored!.Name);
    }

    [Fact]
    public async Task FindMembershipAsync_SoftDeletedMember_ReturnsNull()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        await SoftDeleteMemberAsync(workspaceId, ownerId);

        // Act
        await using var db = fixture.CreateDbContext();
        var membership = await new WorkspaceRepository(db, fixture.Mapper)
            .FindMembershipAsync(workspaceId, ownerId, CancellationToken.None);

        // Assert
        Assert.Null(membership);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesWorkspace()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new WorkspaceRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToWorkspaceMembers()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var membership = await new WorkspaceRepository(assertDb, fixture.Mapper)
            .FindMembershipAsync(workspaceId, ownerId, CancellationToken.None);
        Assert.Null(membership);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToFinancialAccounts()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var account = await TestData.FindAccountAsync(assertDb, fixture.Mapper, accountId, workspaceId);
        Assert.Null(account);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToCategories()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var category = await TestData.FindCategoryAsync(assertDb, fixture.Mapper, categoryId, workspaceId);
        Assert.Null(category);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToTransactions()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId);
        var transactionId = await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryId, ownerId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var transaction = await TestData.FindTransactionAsync(assertDb, fixture.Mapper, transactionId, workspaceId);
        Assert.Null(transaction);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToInvitations()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var invitationId = await AddInvitationAsync(workspaceId, ownerId);

        // Act
        await DeleteWorkspaceAsync(workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var invitation = await new WorkspaceInvitationRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(invitationId, CancellationToken.None);
        Assert.Null(invitation);
    }

    [Fact]
    public async Task DeleteMemberAsync_MakesMemberInvisible()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var memberId = await TestData.SeedUserAsync(fixture);
        await AddMemberAsync(workspaceId, memberId);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new WorkspaceRepository(db, fixture.Mapper)
                .DeleteMemberAsync(workspaceId, memberId, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var membership = await new WorkspaceRepository(assertDb, fixture.Mapper)
            .FindMembershipAsync(workspaceId, memberId, CancellationToken.None);
        Assert.Null(membership);
    }

    [Fact]
    public async Task CountActiveMembersAsync_ExcludesSoftDeletedMembers()
    {
        // Arrange
        var ownerId = await TestData.SeedUserAsync(fixture);
        var workspaceId = await AddWorkspaceAsync(ownerId, "Personal");
        var secondUserId = await TestData.SeedUserAsync(fixture);
        await AddMemberAsync(workspaceId, secondUserId);
        var removedUserId = await TestData.SeedUserAsync(fixture);
        await AddMemberAsync(workspaceId, removedUserId);
        await SoftDeleteMemberAsync(workspaceId, removedUserId);

        // Act
        await using var db = fixture.CreateDbContext();
        var count = await new WorkspaceRepository(db, fixture.Mapper)
            .CountActiveMembersAsync(workspaceId, CancellationToken.None);

        // Assert — owner + secondUser, removedUser excluded
        Assert.Equal(2, count);
    }

    private static (Workspace Workspace, WorkspaceMember Member) NewWorkspace(Guid ownerId, string name)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
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

    private async Task DeleteWorkspaceAsync(Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var repo = new WorkspaceRepository(db, fixture.Mapper);
        var workspace = await repo.FindByIdAsync(workspaceId, CancellationToken.None);
        await repo.DeleteAsync(workspace!, CancellationToken.None);
    }

    private async Task<Guid> AddInvitationAsync(Guid workspaceId, Guid invitedByUserId)
    {
        var invitation = new WorkspaceInvitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = $"{Guid.NewGuid():N}@test.local",
            InvitedByUserId = invitedByUserId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var db = fixture.CreateDbContext();
        await new WorkspaceInvitationRepository(db, fixture.Mapper).UpsertAsync(invitation, CancellationToken.None);

        return invitation.Id;
    }

    private async Task AddMemberAsync(Guid workspaceId, Guid userId)
    {
        await using var db = fixture.CreateDbContext();
        db.WorkspaceMembers.Add(new WorkspaceMemberDb
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = nameof(WorkspaceRole.Member),
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
