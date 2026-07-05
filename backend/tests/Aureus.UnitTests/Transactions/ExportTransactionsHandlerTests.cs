using System.Globalization;
using System.Text;
using Aureus.Domain.Analytics;
using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Domain.Transfers;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.ExportTransactions;

namespace Aureus.UnitTests.Transactions;

public sealed class ExportTransactionsHandlerTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid ToAccountId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static AnalyticsFilter NoFilter() => new(WorkspaceId, null, null, null, null);
    private static AnalyticsFilter TypeFilter(TransactionType type) => new(WorkspaceId, null, null, null, type);

    private static FinancialAccount MakeAccount() => new()
    {
        Id = AccountId,
        WorkspaceId = WorkspaceId,
        Name = "Тинькофф",
        Currency = "RUB",
    };

    private static FinancialAccount MakeToAccount() => new()
    {
        Id = ToAccountId,
        WorkspaceId = WorkspaceId,
        Name = "Наличные",
        Currency = "RUB",
    };

    private static Transfer MakeTransfer(
        long amountMinor = 500000,
        string? note = null,
        DateOnly? occurredAt = null,
        DateTimeOffset? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        WorkspaceId = WorkspaceId,
        FromAccountId = AccountId,
        ToAccountId = ToAccountId,
        CreatedByUserId = Guid.NewGuid(),
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = occurredAt ?? new DateOnly(2026, 6, 15),
        Note = note,
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
    };

    private static Category MakeCategory(TransactionType type = TransactionType.Expense) => new()
    {
        Id = CategoryId,
        WorkspaceId = WorkspaceId,
        Name = "Продукты",
        Type = type,
    };

    private static Transaction MakeTransaction(
        string name = "Пятёрочка",
        long amountMinor = 240000,
        TransactionType type = TransactionType.Expense,
        string? note = null) => new()
    {
        Id = Guid.NewGuid(),
        WorkspaceId = WorkspaceId,
        FinancialAccountId = AccountId,
        CategoryId = CategoryId,
        CreatedByUserId = Guid.NewGuid(),
        Name = name,
        Type = type,
        AmountMinor = amountMinor,
        Currency = "RUB",
        OccurredAt = new DateOnly(2026, 6, 15),
        Note = note,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static ExportTransactionsHandler BuildHandler(
        IReadOnlyList<Transaction> transactions,
        FinancialAccount? account = null,
        Category? category = null,
        IReadOnlyList<Transfer>? transfers = null,
        FinancialAccount? toAccount = null)
    {
        var txRepo = new TransactionRepositoryMock().WithFilterResult(transactions);
        var trRepo = new TransferRepositoryMock().WithFilterResult(transfers ?? []);
        var accounts = new List<FinancialAccount>();
        if (account is not null) accounts.Add(account);
        if (toAccount is not null) accounts.Add(toAccount);
        var accRepo = new FinancialAccountRepositoryMock().WithAccounts(WorkspaceId, accounts);
        var catRepo = new CategoryRepositoryMock().WithAllIncludingDeleted(category is null ? [] : [category]);

        return new ExportTransactionsHandler(txRepo.Object, trRepo.Object, accRepo.Object, catRepo.Object);
    }

    private static string[] ParseCsv(byte[] bytes)
    {
        // Strip UTF-8 BOM if present
        var text = Encoding.UTF8.GetString(bytes).TrimStart('﻿');
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public async Task Handle_EmptyWorkspace_ReturnsHeaderOnly()
    {
        // Arrange
        var handler = BuildHandler([]);

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var lines = ParseCsv(bytes);
        Assert.Single(lines);
        Assert.Equal("date,type,amount,currency,account,toAccount,category,name,note", lines[0].Trim());
    }

    [Fact]
    public async Task Handle_SingleTransaction_WritesCorrectRow()
    {
        // Arrange
        var handler = BuildHandler([MakeTransaction()], MakeAccount(), MakeCategory());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var lines = ParseCsv(bytes);
        Assert.Equal(2, lines.Length);
        var fields = lines[1].Trim().Split(',');
        Assert.Equal("2026-06-15", fields[0]);
        Assert.Equal("Expense", fields[1]);
        Assert.Equal("2400.00", fields[2]);
        Assert.Equal("RUB", fields[3]);
        Assert.Equal("Тинькофф", fields[4]);
        Assert.Equal("", fields[5]);
        Assert.Equal("Продукты", fields[6]);
        Assert.Equal("Пятёрочка", fields[7]);
        Assert.Equal("", fields[8]);
    }

    [Fact]
    public async Task Handle_AmountConvertedFromMinorUnits()
    {
        // Arrange
        var handler = BuildHandler([MakeTransaction(amountMinor: 8_500_000)], MakeAccount(), MakeCategory());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var lines = ParseCsv(bytes);
        var fields = lines[1].Trim().Split(',');
        Assert.Equal("85000.00", fields[2]);
    }

    [Fact]
    public async Task Handle_FormulaInjection_PrefixesWithSpace()
    {
        // Arrange
        var tx = MakeTransaction(name: "=SUM(A1)", note: "+1234");
        var handler = BuildHandler([tx], MakeAccount(), MakeCategory());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var line = ParseCsv(bytes)[1];
        Assert.Contains(" =SUM(A1)", line);
        Assert.Contains(" +1234", line);
    }

    [Fact]
    public async Task Handle_UnknownAccount_FallsBackToId()
    {
        // Arrange — account repo returns empty list
        var handler = BuildHandler([MakeTransaction()], account: null, MakeCategory());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert — account field contains the Guid string
        var line = ParseCsv(bytes)[1];
        Assert.Contains(AccountId.ToString(), line);
    }

    [Fact]
    public async Task Handle_HasBom()
    {
        // Arrange
        var handler = BuildHandler([]);

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert — UTF-8 BOM: EF BB BF
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public async Task Handle_NotePresent_WritesNote()
    {
        // Arrange
        var handler = BuildHandler([MakeTransaction(note: "Годовая подписка")], MakeAccount(), MakeCategory());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var line = ParseCsv(bytes)[1];
        Assert.Contains("Годовая подписка", line);
    }

    [Fact]
    public async Task Handle_AmountUsesDotSeparator()
    {
        // Arrange
        var handler = BuildHandler([MakeTransaction(amountMinor: 150010)], MakeAccount(), MakeCategory());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var fields = ParseCsv(bytes)[1].Trim().Split(',');
        Assert.Equal("1500.10", fields[2]);
        Assert.DoesNotContain(",", fields[2]);
    }

    [Fact]
    public async Task Handle_SingleTransfer_WritesCorrectRow()
    {
        // Arrange
        var handler = BuildHandler([], MakeAccount(), transfers: [MakeTransfer()], toAccount: MakeToAccount());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var lines = ParseCsv(bytes);
        Assert.Equal(2, lines.Length);
        var fields = lines[1].Trim().Split(',');
        Assert.Equal("2026-06-15", fields[0]);
        Assert.Equal("Transfer", fields[1]);
        Assert.Equal("5000.00", fields[2]);
        Assert.Equal("RUB", fields[3]);
        Assert.Equal("Тинькофф", fields[4]);
        Assert.Equal("Наличные", fields[5]);
        Assert.Equal("", fields[6]);
        Assert.Equal("", fields[7]);
        Assert.Equal("", fields[8]);
    }

    [Fact]
    public async Task Handle_TypeFilterSet_ExcludesTransfers()
    {
        // Arrange
        var handler = BuildHandler(
            [MakeTransaction()], MakeAccount(), MakeCategory(),
            transfers: [MakeTransfer()], toAccount: MakeToAccount());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(TypeFilter(TransactionType.Expense)), CancellationToken.None);

        // Assert — only the transaction row, no transfer row
        var lines = ParseCsv(bytes);
        Assert.Equal(2, lines.Length);
        Assert.DoesNotContain("Transfer", lines[1]);
    }

    [Fact]
    public async Task Handle_TransactionAndTransfer_OrdersByOccurredAtDescending()
    {
        // Arrange
        var olderTransaction = MakeTransaction();
        var newerTransfer = MakeTransfer(occurredAt: new DateOnly(2026, 6, 20));
        var handler = BuildHandler(
            [olderTransaction], MakeAccount(), MakeCategory(),
            transfers: [newerTransfer], toAccount: MakeToAccount());

        // Act
        var bytes = await handler.Handle(new ExportTransactionsQuery(NoFilter()), CancellationToken.None);

        // Assert
        var lines = ParseCsv(bytes);
        Assert.Equal(3, lines.Length);
        Assert.Contains("Transfer", lines[1]);
        Assert.Contains("Expense", lines[2]);
    }
}
