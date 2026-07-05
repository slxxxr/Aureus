using Aureus.Postgres.Implementations;
using Aureus.Domain.Transfers;
using Aureus.IntegrationTests.Common;

namespace Aureus.IntegrationTests.Transfers;

[Collection(nameof(PostgresCollection))]
public sealed class TransferRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ValidTransfer_PersistsTransfer()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var transfer = NewTransfer(workspaceId, fromAccountId, toAccountId, userId, amountMinor: 50_00, note: "moved money");

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new TransferRepository(db, fixture.Mapper).AddAsync(transfer, CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransferRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transfer.Id, workspaceId, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(transfer.AmountMinor, stored!.AmountMinor);
        Assert.Equal(transfer.FromAccountId, stored.FromAccountId);
        Assert.Equal(transfer.ToAccountId, stored.ToAccountId);
        Assert.Equal("moved money", stored.Note);
    }

    [Fact]
    public async Task AddAsync_ValidTransfer_UpdatesBothAccountBalances()
    {
        // Arrange
        const long fromInitialBalance = 100_00;
        const long toInitialBalance = 0;
        const long amountMinor = 30_00;

        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: fromInitialBalance);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: toInitialBalance);
        var transfer = NewTransfer(workspaceId, fromAccountId, toAccountId, userId, amountMinor: amountMinor);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            await new TransferRepository(db, fixture.Mapper).AddAsync(transfer, CancellationToken.None);
        }

        // Assert
        Assert.Equal(fromInitialBalance - amountMinor, await GetAccountBalanceAsync(fromAccountId, workspaceId));
        Assert.Equal(toInitialBalance + amountMinor, await GetAccountBalanceAsync(toAccountId, workspaceId));
    }

    [Fact]
    public async Task FindByIdAsync_SoftDeletedTransfer_ReturnsNull()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var transferId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId);

        // Act
        await DeleteTransferAsync(transferId, workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransferRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transferId, workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task FindByIdAsync_WrongWorkspace_ReturnsNull()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var (otherWorkspaceId, _) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var transferId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId);

        // Act
        await using var db = fixture.CreateDbContext();
        var stored = await new TransferRepository(db, fixture.Mapper)
            .FindByIdAsync(transferId, otherWorkspaceId, CancellationToken.None);

        // Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_SoftDeletedTransfer_ExcludesIt()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var liveId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId);
        var deletedId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId);
        await DeleteTransferAsync(deletedId, workspaceId);

        // Act
        await using var db = fixture.CreateDbContext();
        var transfers = await new TransferRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Single(transfers);
        Assert.Equal(liveId, transfers[0].Id);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_ReturnsTransfersOrderedByOccurredAtDescending()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var olderId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId, occurredAt: now.AddDays(-2));
        var newerId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId, occurredAt: now);

        // Act
        await using var db = fixture.CreateDbContext();
        var transfers = await new TransferRepository(db, fixture.Mapper)
            .GetByWorkspaceIdAsync(workspaceId, CancellationToken.None);

        // Assert
        Assert.Equal([newerId, olderId], transfers.Select(t => t.Id));
    }

    [Fact]
    public async Task UpdateAsync_ValidTransfer_UpdatesFields()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var transferId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new TransferRepository(db, fixture.Mapper);
            var transfer = await repo.FindByIdAsync(transferId, workspaceId, CancellationToken.None);
            transfer!.Note = "corrected note";
            transfer.UpdatedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(transfer, new Dictionary<Guid, long>(), CancellationToken.None);
        }

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransferRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transferId, workspaceId, CancellationToken.None);
        Assert.Equal("corrected note", stored!.Note);
    }

    [Fact]
    public async Task UpdateAsync_AmountChanged_UpdatesBothAccountBalances()
    {
        // Arrange
        const long fromInitialBalance = 100_00;
        const long toInitialBalance = 0;
        const long oldAmountMinor = 30_00;
        const long newAmountMinor = 50_00;
        var amountDelta = newAmountMinor - oldAmountMinor;

        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: fromInitialBalance);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: toInitialBalance);
        var transferId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId, amountMinor: oldAmountMinor);

        // Act
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new TransferRepository(db, fixture.Mapper);
            var transfer = await repo.FindByIdAsync(transferId, workspaceId, CancellationToken.None);
            transfer!.AmountMinor = newAmountMinor;
            transfer.UpdatedAt = DateTimeOffset.UtcNow;
            var deltas = new Dictionary<Guid, long> { [fromAccountId] = -amountDelta, [toAccountId] = amountDelta };
            await repo.UpdateAsync(transfer, deltas, CancellationToken.None);
        }

        // Assert
        Assert.Equal(fromInitialBalance - oldAmountMinor - amountDelta, await GetAccountBalanceAsync(fromAccountId, workspaceId));
        Assert.Equal(toInitialBalance + oldAmountMinor + amountDelta, await GetAccountBalanceAsync(toAccountId, workspaceId));
    }

    [Fact]
    public async Task UpdateAsync_FromAccountChanged_UpdatesOldAndNewFromAccountBalances()
    {
        // Arrange
        const long oldFromInitialBalance = 100_00;
        const long newFromInitialBalance = 50_00;
        const long toInitialBalance = 0;
        const long amountMinor = 30_00;

        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var oldFromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: oldFromInitialBalance);
        var newFromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: newFromInitialBalance);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: toInitialBalance);
        var transferId = await AddTransferAsync(workspaceId, oldFromAccountId, toAccountId, userId, amountMinor: amountMinor);

        // Act — move the transfer's source from oldFromAccount to newFromAccount, amount unchanged
        await using (var db = fixture.CreateDbContext())
        {
            var repo = new TransferRepository(db, fixture.Mapper);
            var transfer = await repo.FindByIdAsync(transferId, workspaceId, CancellationToken.None);
            transfer!.FromAccountId = newFromAccountId;
            transfer.UpdatedAt = DateTimeOffset.UtcNow;
            var deltas = new Dictionary<Guid, long>
            {
                [oldFromAccountId] = amountMinor,   // reverse old debit
                [newFromAccountId] = -amountMinor,  // apply new debit
                [toAccountId] = 0,
            };
            await repo.UpdateAsync(transfer, deltas, CancellationToken.None);
        }

        // Assert
        Assert.Equal(oldFromInitialBalance, await GetAccountBalanceAsync(oldFromAccountId, workspaceId));
        Assert.Equal(newFromInitialBalance - amountMinor, await GetAccountBalanceAsync(newFromAccountId, workspaceId));
        Assert.Equal(toInitialBalance + amountMinor, await GetAccountBalanceAsync(toAccountId, workspaceId));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesTransfer()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: 100_00);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var transferId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId);

        // Act
        await DeleteTransferAsync(transferId, workspaceId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await new TransferRepository(assertDb, fixture.Mapper)
            .FindByIdAsync(transferId, workspaceId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_ReversesBothAccountBalances()
    {
        // Arrange
        const long fromInitialBalance = 100_00;
        const long toInitialBalance = 0;
        const long amountMinor = 30_00;

        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var fromAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: fromInitialBalance);
        var toAccountId = await TestData.SeedAccountAsync(fixture, workspaceId, initialBalance: toInitialBalance);
        var transferId = await AddTransferAsync(workspaceId, fromAccountId, toAccountId, userId, amountMinor: amountMinor);

        // Act
        await DeleteTransferAsync(transferId, workspaceId);

        // Assert
        Assert.Equal(fromInitialBalance, await GetAccountBalanceAsync(fromAccountId, workspaceId));
        Assert.Equal(toInitialBalance, await GetAccountBalanceAsync(toAccountId, workspaceId));
    }

    private static Transfer NewTransfer(
        Guid workspaceId,
        Guid fromAccountId,
        Guid toAccountId,
        Guid createdByUserId,
        long amountMinor = 10_00,
        string? note = null,
        DateOnly? occurredAt = null) => new()
    {
        Id = Guid.NewGuid(),
        WorkspaceId = workspaceId,
        FromAccountId = fromAccountId,
        ToAccountId = toAccountId,
        CreatedByUserId = createdByUserId,
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = occurredAt ?? DateOnly.FromDateTime(DateTime.UtcNow),
        Note = note,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private async Task<Guid> AddTransferAsync(
        Guid workspaceId,
        Guid fromAccountId,
        Guid toAccountId,
        Guid createdByUserId,
        long amountMinor = 10_00,
        DateOnly? occurredAt = null)
    {
        var transfer = NewTransfer(workspaceId, fromAccountId, toAccountId, createdByUserId, amountMinor, occurredAt: occurredAt);

        await using var db = fixture.CreateDbContext();
        await new TransferRepository(db, fixture.Mapper).AddAsync(transfer, CancellationToken.None);

        return transfer.Id;
    }

    private async Task DeleteTransferAsync(Guid transferId, Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var repo = new TransferRepository(db, fixture.Mapper);
        var transfer = await repo.FindByIdAsync(transferId, workspaceId, CancellationToken.None);
        await repo.DeleteAsync(transfer!, CancellationToken.None);
    }

    private async Task<long> GetAccountBalanceAsync(Guid accountId, Guid workspaceId)
    {
        await using var db = fixture.CreateDbContext();
        var account = await TestData.FindAccountAsync(db, fixture.Mapper, accountId, workspaceId);
        return account!.CurrentBalanceMinor;
    }
}
