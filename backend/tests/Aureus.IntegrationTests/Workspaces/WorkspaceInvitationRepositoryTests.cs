using Aureus.Domain.Workspaces;
using Aureus.IntegrationTests.Common;
using Aureus.Postgres.Implementations;

namespace Aureus.IntegrationTests.Workspaces;

[Collection(nameof(PostgresCollection))]
public sealed class WorkspaceInvitationRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static WorkspaceInvitation NewInvitation(
        Guid workspaceId, Guid invitedByUserId, string? email = null, DateTimeOffset? expiresAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = email ?? $"{Guid.NewGuid():N}@test.local",
            InvitedByUserId = invitedByUserId,
            TokenHash = "hash",
            ExpiresAt = expiresAt ?? Now.AddDays(7),
            CreatedAt = Now,
        };

    [Fact]
    public async Task UpsertAsync_NewInvitation_PersistsIt()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var invitation = NewInvitation(workspaceId, ownerId);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);

        // Act
        await repository.UpsertAsync(invitation, CancellationToken.None);

        // Assert
        var stored = await repository.FindByIdAsync(invitation.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(invitation.Email, stored!.Email);
    }

    [Fact]
    public async Task UpsertAsync_ExistingWorkspaceAndEmail_UpdatesInPlaceInsteadOfInserting()
    {
        // Resend builds a fresh WorkspaceInvitation (new Id) for the same (workspace, email) pair —
        // UpsertAsync must key off the natural key, not Id, or this collides with the unique index.

        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var email = $"{Guid.NewGuid():N}@test.local";
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        var original = NewInvitation(workspaceId, ownerId, email: email, expiresAt: Now.AddDays(7));
        await repository.UpsertAsync(original, CancellationToken.None);

        var resend = NewInvitation(workspaceId, ownerId, email: email, expiresAt: Now.AddDays(14));
        resend.TokenHash = "new-hash";

        // Act
        await repository.UpsertAsync(resend, CancellationToken.None);

        // Assert
        var stored = await repository.FindPendingAsync(workspaceId, email, CancellationToken.None);
        Assert.Equal(original.Id, stored!.Id);
        Assert.Equal("new-hash", stored.TokenHash);
        Assert.Equal(Now.AddDays(14), stored.ExpiresAt);
    }

    [Fact]
    public async Task FindByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);

        // Act
        var stored = await repository.FindByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task FindPendingAsync_Match_ReturnsInvitation()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var invitation = NewInvitation(workspaceId, ownerId, email: "invitee@test.local");
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        await repository.UpsertAsync(invitation, CancellationToken.None);

        // Act
        var stored = await repository.FindPendingAsync(workspaceId, "invitee@test.local", CancellationToken.None);

        // Assert
        Assert.NotNull(stored);
        Assert.Equal(invitation.Id, stored!.Id);
    }

    [Fact]
    public async Task FindPendingAsync_ExpiredInvitation_StillReturnsIt()
    {
        // FindPendingAsync backs the resend decision in the invite handler — an expired row must
        // still be found so a resend updates it in place instead of colliding with the unique index.

        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var email = $"{Guid.NewGuid():N}@test.local";
        var invitation = NewInvitation(workspaceId, ownerId, email: email, expiresAt: Now.AddDays(-1));
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        await repository.UpsertAsync(invitation, CancellationToken.None);

        // Act
        var stored = await repository.FindPendingAsync(workspaceId, email, CancellationToken.None);

        // Assert
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task FindPendingAsync_NoMatch_ReturnsNull()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);

        // Act
        var stored = await repository.FindPendingAsync(workspaceId, "nobody@test.local", CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetPendingForWorkspaceAsync_ExcludesExpired()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        await repository.UpsertAsync(
            NewInvitation(workspaceId, ownerId, email: "active@test.local", expiresAt: Now.AddDays(7)),
            CancellationToken.None);
        await repository.UpsertAsync(
            NewInvitation(workspaceId, ownerId, email: "expired@test.local", expiresAt: Now.AddDays(-1)),
            CancellationToken.None);

        // Act
        var pending = await repository.GetPendingForWorkspaceAsync(workspaceId, Now, CancellationToken.None);

        // Assert
        Assert.Single(pending);
        Assert.Equal("active@test.local", pending[0].Email);
    }

    [Fact]
    public async Task GetPendingForEmailAsync_ReturnsWorkspaceName()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var email = $"{Guid.NewGuid():N}@test.local";
        await using var db = fixture.CreateDbContext();
        var workspace = await new WorkspaceRepository(db, fixture.Mapper)
            .FindByIdAsync(workspaceId, CancellationToken.None);
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        await repository.UpsertAsync(NewInvitation(workspaceId, ownerId, email: email), CancellationToken.None);

        // Act
        var pending = await repository.GetPendingForEmailAsync(email, Now, CancellationToken.None);

        // Assert
        Assert.Single(pending);
        Assert.Equal(workspaceId, pending[0].WorkspaceId);
        Assert.Equal(workspace!.Name, pending[0].WorkspaceName);
    }

    [Fact]
    public async Task CountActiveAsync_ExcludesExpired()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        await repository.UpsertAsync(
            NewInvitation(workspaceId, ownerId, email: "active@test.local", expiresAt: Now.AddDays(7)),
            CancellationToken.None);
        await repository.UpsertAsync(
            NewInvitation(workspaceId, ownerId, email: "expired@test.local", expiresAt: Now.AddDays(-1)),
            CancellationToken.None);

        // Act
        var count = await repository.CountActiveAsync(workspaceId, Now, CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesInvitation()
    {
        // Arrange
        var (workspaceId, ownerId) = await TestData.SeedWorkspaceAsync(fixture);
        var invitation = NewInvitation(workspaceId, ownerId);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(db, fixture.Mapper);
        await repository.UpsertAsync(invitation, CancellationToken.None);

        // Act
        await repository.DeleteAsync(invitation.Id, CancellationToken.None);

        // Assert
        var stored = await repository.FindByIdAsync(invitation.Id, CancellationToken.None);
        Assert.Null(stored);
    }
}
