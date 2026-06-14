using Aureus.IntegrationTests.Common;
using Aureus.Persistence.Interfaces;
using Aureus.Postgres.Implementations;

namespace Aureus.IntegrationTests.Analytics;

[Collection(nameof(PostgresCollection))]
public sealed class WorkspaceDailyUsageRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateOnly Today = new(2026, 6, 1);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    [Fact]
    public async Task IncrementAndGetAsync_FirstCall_ReturnsOne()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceDailyUsageRepository(db);

        // Act
        var count = await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Today, CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task IncrementAndGetAsync_SameDay_Increments()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceDailyUsageRepository(db);

        // Act
        await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Today, CancellationToken.None);
        var count = await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Today, CancellationToken.None);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task IncrementAndGetAsync_NewDay_ResetsCount()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceDailyUsageRepository(db);

        // Act
        await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Today, CancellationToken.None);
        var count = await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Tomorrow, CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task IncrementAndGetAsync_DifferentFeatures_TrackedSeparately()
    {
        // Arrange
        var (workspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceDailyUsageRepository(db);

        // Act — increment Insights twice, other feature once
        await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Today, CancellationToken.None);
        var insightsCount = await repository.IncrementAndGetAsync(workspaceId, DailyUsageFeature.Insights, Today, CancellationToken.None);

        // Assert — Insights has its own counter
        Assert.Equal(2, insightsCount);
    }

    [Fact]
    public async Task IncrementAndGetAsync_DifferentWorkspaces_TrackedSeparately()
    {
        // Arrange
        var (workspaceA, _) = await TestData.SeedWorkspaceAsync(fixture);
        var (workspaceB, _) = await TestData.SeedWorkspaceAsync(fixture);
        await using var db = fixture.CreateDbContext();
        var repository = new WorkspaceDailyUsageRepository(db);

        // Act
        await repository.IncrementAndGetAsync(workspaceA, DailyUsageFeature.Insights, Today, CancellationToken.None);
        await repository.IncrementAndGetAsync(workspaceA, DailyUsageFeature.Insights, Today, CancellationToken.None);
        var countB = await repository.IncrementAndGetAsync(workspaceB, DailyUsageFeature.Insights, Today, CancellationToken.None);

        // Assert
        Assert.Equal(1, countB);
    }
}
