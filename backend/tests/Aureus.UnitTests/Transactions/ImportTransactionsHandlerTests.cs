using System.Text;
using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Transactions.ImportTransactions;
using static Aureus.UseCases.Transactions.ImportTransactions.ImportRowErrorCode;

namespace Aureus.UnitTests.Transactions;

public sealed class ImportTransactionsHandlerTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid ToAccountId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static FinancialAccount MakeAccount(string name = "Тинькофф", string currency = "RUB") => new()
    {
        Id = AccountId,
        WorkspaceId = WorkspaceId,
        Name = name,
        Currency = currency,
    };

    private static FinancialAccount MakeToAccount(string name = "Наличные", string currency = "RUB") => new()
    {
        Id = ToAccountId,
        WorkspaceId = WorkspaceId,
        Name = name,
        Currency = currency,
    };

    private static Category MakeCategory(string name = "Продукты", TransactionType type = TransactionType.Expense) => new()
    {
        Id = CategoryId,
        WorkspaceId = WorkspaceId,
        Name = name,
        Type = type,
    };

    private static byte[] Csv(string content) =>
        Encoding.UTF8.GetBytes(content);

    private static ImportPreviewHandler BuildPreviewHandler(
        FinancialAccount? account = null, Category? category = null, FinancialAccount? toAccount = null)
    {
        var accounts = new List<FinancialAccount>();
        if (account is not null) accounts.Add(account);
        if (toAccount is not null) accounts.Add(toAccount);
        var accRepo = new FinancialAccountRepositoryMock().WithAccounts(WorkspaceId, accounts);
        var catRepo = new CategoryRepositoryMock().WithCategories(WorkspaceId, category is null ? [] : [category]);
        return new ImportPreviewHandler(accRepo.Object, catRepo.Object);
    }

    private static (CommitImportHandler handler, ImportRepositoryMock importMock) BuildCommitHandler(
        FinancialAccount? account = null, Category? category = null, FinancialAccount? toAccount = null)
    {
        var accounts = new List<FinancialAccount>();
        if (account is not null) accounts.Add(account);
        if (toAccount is not null) accounts.Add(toAccount);
        var accRepo = new FinancialAccountRepositoryMock().WithAccounts(WorkspaceId, accounts);
        var catRepo = new CategoryRepositoryMock().WithCategories(WorkspaceId, category is null ? [] : [category]);
        var importMock = new ImportRepositoryMock().CapturingAddBulk();
        return (new CommitImportHandler(accRepo.Object, catRepo.Object, importMock.Object), importMock);
    }

    // ─── preview ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Preview_ValidRow_ReturnsIsValid()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,category,name,note\n2026-06-01,Expense,1500.00,Тинькофф,Продукты,Пятёрочка,");
        var handler = BuildPreviewHandler(MakeAccount(), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ValidCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(result.Rows[0].IsValid);
    }

    [Fact]
    public async Task Handle_Preview_InvalidDate_ReturnsError()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,category,name,note\n01/06/2026,Expense,1500.00,Тинькофф,Продукты,Пятёрочка,");
        var handler = BuildPreviewHandler(MakeAccount(), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ValidCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.False(result.Rows[0].IsValid);
        Assert.Equal(InvalidDate, result.Rows[0].ErrorCode);
    }

    [Fact]
    public async Task Handle_Preview_UnknownAccount_ReturnsError()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,category,name,note\n2026-06-01,Expense,1500.00,НеизвестныйСчёт,Продукты,Пятёрочка,");
        var handler = BuildPreviewHandler(MakeAccount("Тинькофф"), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.False(result.Rows[0].IsValid);
        Assert.Equal(AccountNotFound, result.Rows[0].ErrorCode);
        Assert.Equal("НеизвестныйСчёт", result.Rows[0].ErrorSubject);
    }

    [Fact]
    public async Task Handle_Preview_CategoryTypeMismatch_ReturnsError()
    {
        // Arrange — category is Expense, row is Income
        var csv = Csv("date,type,amount,account,category,name,note\n2026-06-01,Income,1500.00,Тинькофф,Продукты,Зарплата,");
        var handler = BuildPreviewHandler(MakeAccount(), MakeCategory("Продукты", TransactionType.Expense));

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.False(result.Rows[0].IsValid);
    }

    [Fact]
    public async Task Handle_Preview_SemicolonDelimiter_ParsesCorrectly()
    {
        // Arrange — semicolon-separated (RU Excel format)
        var csv = Csv("date;type;amount;account;category;name;note\n2026-06-01;Expense;1500.00;Тинькофф;Продукты;Пятёрочка;");
        var handler = BuildPreviewHandler(MakeAccount(), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ValidCount);
    }

    [Fact]
    public async Task Handle_Preview_WithCurrencyColumn_IgnoresCurrency()
    {
        // Arrange — file has currency column (exported format), should be ignored
        var csv = Csv("date,type,amount,currency,account,category,name,note\n2026-06-01,Expense,1500.00,USD,Тинькофф,Продукты,Пятёрочка,");
        var handler = BuildPreviewHandler(MakeAccount("Тинькофф", "RUB"), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert — currency column is ignored, account RUB used
        Assert.True(result.Rows[0].IsValid);
    }

    [Fact]
    public async Task Handle_Preview_NegativeAmount_ReturnsError()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,category,name,note\n2026-06-01,Expense,-500.00,Тинькофф,Продукты,Тест,");
        var handler = BuildPreviewHandler(MakeAccount(), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.False(result.Rows[0].IsValid);
    }

    [Fact]
    public async Task Handle_Preview_MultipleRows_AllValidated()
    {
        // Arrange — two rows: first valid, second invalid date
        var csv = Csv(
            "date,type,amount,account,category,name,note\n" +
            "2026-06-01,Expense,1500.00,Тинькофф,Продукты,Пятёрочка,\n" +
            "bad-date,Expense,500.00,Тинькофф,Продукты,Аптека,");
        var handler = BuildPreviewHandler(MakeAccount(), MakeCategory());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1, result.ValidCount);
        Assert.Equal(1, result.ErrorCount);
    }

    // ─── preview: transfers ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Preview_ValidTransferRow_ReturnsIsValid()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,toAccount,category,name,note\n2026-06-01,Transfer,5000.00,Тинькофф,Наличные,,,");
        var handler = BuildPreviewHandler(MakeAccount(), toAccount: MakeToAccount());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ValidCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(result.Rows[0].IsValid);
    }

    [Fact]
    public async Task Handle_Preview_TransferMissingToAccount_ReturnsToAccountNotFound()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,toAccount,category,name,note\n2026-06-01,Transfer,5000.00,Тинькофф,НеизвестныйСчёт,,,");
        var handler = BuildPreviewHandler(MakeAccount());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.False(result.Rows[0].IsValid);
        Assert.Equal(ToAccountNotFound, result.Rows[0].ErrorCode);
        Assert.Equal("НеизвестныйСчёт", result.Rows[0].ErrorSubject);
    }

    [Fact]
    public async Task Handle_Preview_TransferSameAccount_ReturnsSameAccount()
    {
        // Arrange — account and toAccount both resolve to the same financial account
        var csv = Csv("date,type,amount,account,toAccount,category,name,note\n2026-06-01,Transfer,5000.00,Тинькофф,Тинькофф,,,");
        var handler = BuildPreviewHandler(MakeAccount());

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.False(result.Rows[0].IsValid);
        Assert.Equal(SameAccount, result.Rows[0].ErrorCode);
    }

    [Fact]
    public async Task Handle_Preview_TransferCurrencyMismatch_ReturnsCurrencyMismatch()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,toAccount,category,name,note\n2026-06-01,Transfer,5000.00,Тинькофф,Наличные,,,");
        var handler = BuildPreviewHandler(MakeAccount(currency: "RUB"), toAccount: MakeToAccount(currency: "USD"));

        // Act
        var result = await handler.Handle(new ImportPreviewQuery(WorkspaceId, csv), CancellationToken.None);

        // Assert
        Assert.False(result.Rows[0].IsValid);
        Assert.Equal(CurrencyMismatch, result.Rows[0].ErrorCode);
    }

    // ─── commit ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Commit_ValidFile_SavesTransactionsAndReturnsCount()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,category,name,note\n2026-06-01,Expense,1500.00,Тинькофф,Продукты,Пятёрочка,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount(), MakeCategory());

        // Act
        var count = await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
        Assert.NotNull(importMock.AddedTransactions);
        Assert.Single(importMock.AddedTransactions!);
    }

    [Fact]
    public async Task Handle_Commit_AmountConvertedToMinorUnits()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,category,name,note\n2026-06-01,Expense,1500.50,Тинькофф,Продукты,Тест,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount(), MakeCategory());

        // Act
        await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(150050L, importMock.AddedTransactions![0].AmountMinor);
    }

    [Fact]
    public async Task Handle_Commit_CurrencyTakenFromAccount()
    {
        // Arrange
        var csv = Csv("date,type,amount,currency,account,category,name,note\n2026-06-01,Expense,500.00,USD,Тинькофф,Продукты,Тест,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount("Тинькофф", "RUB"), MakeCategory());

        // Act
        await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert — currency comes from account, not CSV column
        Assert.Equal("RUB", importMock.AddedTransactions![0].Currency);
    }

    [Fact]
    public async Task Handle_Commit_BalanceDeltaCalculatedCorrectly()
    {
        // Arrange — one income +3000, one expense -1500 on same account
        var csv = Csv(
            "date,type,amount,account,category,name,note\n" +
            "2026-06-01,Income,3000.00,Тинькофф,Продукты,Зарплата,\n" +
            "2026-06-02,Expense,1500.00,Тинькофф,Продукты,Еда,");
        var incomeCategory = MakeCategory("Продукты", TransactionType.Income);
        var expenseCategory = new Category { Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, Name = "Продукты", Type = TransactionType.Expense };
        var accRepo = new FinancialAccountRepositoryMock().WithAccounts(WorkspaceId, [MakeAccount()]);
        var catRepo = new CategoryRepositoryMock().WithCategories(WorkspaceId, [incomeCategory, expenseCategory]);
        var importMock = new ImportRepositoryMock().CapturingAddBulk();
        var handler = new CommitImportHandler(accRepo.Object, catRepo.Object, importMock.Object);

        // Act
        await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert — net delta: +300000 - 150000 = 150000
        Assert.Equal(150000L, importMock.AddedTransactionDeltas![AccountId]);
    }

    [Fact]
    public async Task Handle_Commit_HasErrors_ThrowsTransactionException()
    {
        // Arrange — invalid date in row
        var csv = Csv("date,type,amount,account,category,name,note\nbad,Expense,500.00,Тинькофф,Продукты,Тест,");
        var (handler, _) = BuildCommitHandler(MakeAccount(), MakeCategory());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TransactionException>(
            () => handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None));
        Assert.Equal(TransactionErrorCode.ImportHasErrors, ex.Code);
    }

    [Fact]
    public async Task Handle_Commit_OrderWithinDayPreservedViaCreatedAt()
    {
        // Arrange — two rows with same date
        var csv = Csv(
            "date,type,amount,account,category,name,note\n" +
            "2026-06-01,Expense,100.00,Тинькофф,Продукты,Первая,\n" +
            "2026-06-01,Expense,200.00,Тинькофф,Продукты,Вторая,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount(), MakeCategory());

        // Act
        await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert — second row has later CreatedAt
        Assert.True(importMock.AddedTransactions![1].CreatedAt > importMock.AddedTransactions![0].CreatedAt);
    }

    // ─── commit: transfers ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Commit_ValidTransferFile_SavesTransferAndReturnsCount()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,toAccount,category,name,note\n2026-06-01,Transfer,5000.00,Тинькофф,Наличные,,,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount(), toAccount: MakeToAccount());

        // Act
        var count = await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
        Assert.NotNull(importMock.AddedTransfers);
        Assert.Single(importMock.AddedTransfers!);
        Assert.Equal(500000L, importMock.AddedTransfers![0].AmountMinor);
        Assert.Equal(AccountId, importMock.AddedTransfers![0].FromAccountId);
        Assert.Equal(ToAccountId, importMock.AddedTransfers![0].ToAccountId);
    }

    [Fact]
    public async Task Handle_Commit_TransferBalanceDeltaCalculatedCorrectly()
    {
        // Arrange
        var csv = Csv("date,type,amount,account,toAccount,category,name,note\n2026-06-01,Transfer,5000.00,Тинькофф,Наличные,,,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount(), toAccount: MakeToAccount());

        // Act
        await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(-500000L, importMock.AddedTransferDeltas![AccountId]);
        Assert.Equal(500000L, importMock.AddedTransferDeltas![ToAccountId]);
    }

    [Fact]
    public async Task Handle_Commit_TransactionAndTransferInSameFile_CommitsBothAtomically()
    {
        // Arrange — one Expense row, one Transfer row, same commit
        var csv = Csv(
            "date,type,amount,account,toAccount,category,name,note\n" +
            "2026-06-01,Expense,1500.00,Тинькофф,,Продукты,Пятёрочка,\n" +
            "2026-06-02,Transfer,5000.00,Тинькофф,Наличные,,,");
        var (handler, importMock) = BuildCommitHandler(MakeAccount(), MakeCategory(), MakeToAccount());

        // Act
        var count = await handler.Handle(new CommitImportCommand(WorkspaceId, UserId, csv), CancellationToken.None);

        // Assert
        Assert.Equal(2, count);
        Assert.Single(importMock.AddedTransactions!);
        Assert.Single(importMock.AddedTransfers!);
    }
}
