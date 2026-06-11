using Aureus.Postgres.Implementations;
using Aureus.Domain.Transactions;
using Aureus.IntegrationTests.Common;


using Aureus.Domain.Analytics;

namespace Aureus.IntegrationTests.Analytics;

[Collection(nameof(PostgresCollection))]
public sealed class AnalyticsRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateOnly _march = new(2024, 3, 1);

    [Fact]
    public async Task GetSummaryAsync_GroupsIncomeAndExpensePerCurrency()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var expenseCategory = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        var incomeCategory = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Income);
        const long firstExpense = 100_00;
        const long secondExpense = 50_00;
        const long income = 200_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, expenseCategory, userId, TransactionType.Expense, firstExpense);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, expenseCategory, userId, TransactionType.Expense, secondExpense);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, incomeCategory, userId, TransactionType.Income, income);

        // Act
        await using var db = fixture.CreateDbContext();
        var summary = await new AnalyticsRepository(db)
            .GetSummaryAsync(Filter(workspaceId), CancellationToken.None);

        // Assert
        var row = Assert.Single(summary);
        Assert.Equal("RUB", row.Currency);
        Assert.Equal(income, row.IncomeMinor);
        Assert.Equal(firstExpense + secondExpense, row.ExpensesMinor);
        Assert.Equal(income - firstExpense - secondExpense, row.NetMinor);
    }

    [Fact]
    public async Task GetSummaryAsync_MultipleCurrencies_ReturnsRowPerCurrency()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var category = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const string rub = "RUB";
        const string usd = "USD";
        const long rubExpense = 100_00;
        const long usdExpense = 30_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, rubExpense, rub);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, usdExpense, usd);

        // Act
        await using var db = fixture.CreateDbContext();
        var summary = await new AnalyticsRepository(db)
            .GetSummaryAsync(Filter(workspaceId), CancellationToken.None);

        // Assert
        Assert.Equal(2, summary.Count);
        Assert.Equal(rubExpense, Assert.Single(summary, row => row.Currency == rub).ExpensesMinor);
        Assert.Equal(usdExpense, Assert.Single(summary, row => row.Currency == usd).ExpensesMinor);
    }

    [Fact]
    public async Task GetSummaryAsync_DateRange_ExcludesTransactionsOutsidePeriod()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var category = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long insidePeriod = 100_00;
        const long outsidePeriod = 999_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, insidePeriod, occurredAt: _march.AddDays(14));
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, outsidePeriod, occurredAt: _march.AddMonths(1));

        // Act — half-open [from, to): the April 1 transaction must be excluded
        await using var db = fixture.CreateDbContext();
        var summary = await new AnalyticsRepository(db)
            .GetSummaryAsync(Filter(workspaceId, from: _march, to: _march.AddMonths(1)), CancellationToken.None);

        // Assert
        Assert.Equal(insidePeriod, Assert.Single(summary).ExpensesMinor);
    }

    [Fact]
    public async Task GetSummaryAsync_AccountFilter_LimitsToSelectedAccounts()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountA = await TestData.SeedAccountAsync(fixture, workspaceId);
        var accountB = await TestData.SeedAccountAsync(fixture, workspaceId);
        var category = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long selectedAccountExpense = 100_00;
        const long otherAccountExpense = 50_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountA, category, userId, TransactionType.Expense, selectedAccountExpense);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountB, category, userId, TransactionType.Expense, otherAccountExpense);

        // Act
        await using var db = fixture.CreateDbContext();
        var summary = await new AnalyticsRepository(db)
            .GetSummaryAsync(Filter(workspaceId, accountIds: [accountA]), CancellationToken.None);

        // Assert
        Assert.Equal(selectedAccountExpense, Assert.Single(summary).ExpensesMinor);
    }

    [Fact]
    public async Task GetSummaryAsync_CategoryFilter_LimitsToSelectedCategories()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var selectedCategory = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        var otherCategory = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long selectedCategoryExpense = 100_00;
        const long otherCategoryExpense = 50_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, selectedCategory, userId, TransactionType.Expense, selectedCategoryExpense);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, otherCategory, userId, TransactionType.Expense, otherCategoryExpense);

        // Act
        await using var db = fixture.CreateDbContext();
        var summary = await new AnalyticsRepository(db)
            .GetSummaryAsync(Filter(workspaceId, categoryIds: [selectedCategory]), CancellationToken.None);

        // Assert
        Assert.Equal(selectedCategoryExpense, Assert.Single(summary).ExpensesMinor);
    }

    [Fact]
    public async Task GetBreakdownAsync_ByCategory_SumsPerCategory()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryA = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        var categoryB = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long firstA = 100_00;
        const long secondA = 50_00;
        const long amountB = 20_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryA, userId, TransactionType.Expense, firstA);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryA, userId, TransactionType.Expense, secondA);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryB, userId, TransactionType.Expense, amountB);

        // Act
        await using var db = fixture.CreateDbContext();
        var breakdown = await new AnalyticsRepository(db)
            .GetBreakdownAsync(Filter(workspaceId), BreakdownDimension.Category, CancellationToken.None);

        // Assert
        Assert.Equal(2, breakdown.Count);
        Assert.Equal(firstA + secondA, Assert.Single(breakdown, item => item.Key == categoryA.ToString()).AmountMinor);
        Assert.Equal(amountB, Assert.Single(breakdown, item => item.Key == categoryB.ToString()).AmountMinor);
    }

    [Fact]
    public async Task GetBreakdownAsync_SoftDeletedCategory_KeepsTransactionWithNullLabel()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long amount = 40_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryId, userId, TransactionType.Expense, amount);

        await using (var deleteDb = fixture.CreateDbContext())
        {
            var category = await TestData.FindCategoryAsync(deleteDb, fixture.Mapper, categoryId, workspaceId);
            await new CategoryRepository(deleteDb, fixture.Mapper).DeleteAsync(category!, CancellationToken.None);
        }

        // Act
        await using var db = fixture.CreateDbContext();
        var breakdown = await new AnalyticsRepository(db)
            .GetBreakdownAsync(Filter(workspaceId), BreakdownDimension.Category, CancellationToken.None);

        // Assert
        var item = Assert.Single(breakdown);
        Assert.Equal(categoryId.ToString(), item.Key);
        Assert.Null(item.Label);
        Assert.Equal(amount, item.AmountMinor);
    }

    [Fact]
    public async Task GetBreakdownAsync_ByName_SumsPerName()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var category = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long firstTaxi = 300_00;
        const long secondTaxi = 200_00;
        const long metro = 50_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, firstTaxi, name: "Такси");
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, secondTaxi, name: "Такси");
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, metro, name: "Метро");

        // Act
        await using var db = fixture.CreateDbContext();
        var breakdown = await new AnalyticsRepository(db)
            .GetBreakdownAsync(Filter(workspaceId), BreakdownDimension.Name, CancellationToken.None);

        // Assert
        Assert.Equal(2, breakdown.Count);
        var taxi = Assert.Single(breakdown, item => item.Key == "Такси");
        Assert.Equal("Такси", taxi.Label);
        Assert.Equal(firstTaxi + secondTaxi, taxi.AmountMinor);
        Assert.Equal(metro, Assert.Single(breakdown, item => item.Key == "Метро").AmountMinor);
    }

    [Fact]
    public async Task GetBreakdownAsync_ByName_CaseSensitive_KeepsVariantsSeparate()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var category = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, 100_00, name: "Такси");
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, 50_00, name: "такси");

        // Act
        await using var db = fixture.CreateDbContext();
        var breakdown = await new AnalyticsRepository(db)
            .GetBreakdownAsync(Filter(workspaceId), BreakdownDimension.Name, CancellationToken.None);

        // Assert
        Assert.Equal(2, breakdown.Count);
    }

    [Fact]
    public async Task GetBreakdownAsync_ByName_CategoryFilter_LimitsToCategory()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var transport = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        var food = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long taxi = 300_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, transport, userId, TransactionType.Expense, taxi, name: "Такси");
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, food, userId, TransactionType.Expense, 80_00, name: "Кофе");

        // Act — drill-down into a single category
        await using var db = fixture.CreateDbContext();
        var breakdown = await new AnalyticsRepository(db)
            .GetBreakdownAsync(Filter(workspaceId, categoryIds: [transport]), BreakdownDimension.Name, CancellationToken.None);

        // Assert
        var item = Assert.Single(breakdown);
        Assert.Equal("Такси", item.Key);
        Assert.Equal(taxi, item.AmountMinor);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ByMonth_RollsUpDaysIntoMonthBuckets()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var category = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long firstMarchExpense = 100_00;
        const long secondMarchExpense = 50_00;
        const long aprilExpense = 30_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, firstMarchExpense, occurredAt: _march.AddDays(9));
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, secondMarchExpense, occurredAt: _march.AddDays(19));
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, category, userId, TransactionType.Expense, aprilExpense, occurredAt: _march.AddMonths(1).AddDays(4));

        // Act
        await using var db = fixture.CreateDbContext();
        var series = await new AnalyticsRepository(db)
            .GetTimeSeriesAsync(Filter(workspaceId), TimeInterval.Month, CancellationToken.None);

        // Assert
        Assert.Equal(2, series.Count);
        Assert.Equal(_march, series[0].PeriodStart);
        Assert.Equal(firstMarchExpense + secondMarchExpense, series[0].ExpensesMinor);
        Assert.Equal(_march.AddMonths(1), series[1].PeriodStart);
        Assert.Equal(aprilExpense, series[1].ExpensesMinor);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ByDay_AggregatesSameDayTransactions()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var expenseCategory = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        var incomeCategory = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Income);
        var day = _march.AddDays(9);
        const long expense = 100_00;
        const long income = 70_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, expenseCategory, userId, TransactionType.Expense, expense, occurredAt: day);
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, incomeCategory, userId, TransactionType.Income, income, occurredAt: day);

        // Act
        await using var db = fixture.CreateDbContext();
        var series = await new AnalyticsRepository(db)
            .GetTimeSeriesAsync(Filter(workspaceId), TimeInterval.Day, CancellationToken.None);

        // Assert
        var point = Assert.Single(series);
        Assert.Equal(_march.AddDays(9), point.PeriodStart);
        Assert.Equal(income, point.IncomeMinor);
        Assert.Equal(expense, point.ExpensesMinor);
    }

    [Fact]
    public async Task GetCategoryTimeSeriesAsync_ByMonth_GroupsPerCategoryAndMonth()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryA = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        var categoryB = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long firstMarchA = 100_00;
        const long secondMarchA = 50_00;
        const long marchB = 20_00;
        const long aprilA = 30_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryA, userId, TransactionType.Expense, firstMarchA, occurredAt: _march.AddDays(9));
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryA, userId, TransactionType.Expense, secondMarchA, occurredAt: _march.AddDays(19));
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryB, userId, TransactionType.Expense, marchB, occurredAt: _march.AddDays(9));
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryA, userId, TransactionType.Expense, aprilA, occurredAt: _march.AddMonths(1).AddDays(4));

        // Act
        await using var db = fixture.CreateDbContext();
        var series = await new AnalyticsRepository(db)
            .GetCategoryTimeSeriesAsync(Filter(workspaceId, type: TransactionType.Expense), TimeInterval.Month, CancellationToken.None);

        // Assert
        Assert.Equal(3, series.Count);
        var marchAPoint = Assert.Single(series, point => point.PeriodStart == _march && point.CategoryId == categoryA);
        Assert.Equal(firstMarchA + secondMarchA, marchAPoint.AmountMinor);
        Assert.NotNull(marchAPoint.Label);
        Assert.Equal(marchB, Assert.Single(series, point => point.PeriodStart == _march && point.CategoryId == categoryB).AmountMinor);
        Assert.Equal(aprilA, Assert.Single(series, point => point.PeriodStart == _march.AddMonths(1) && point.CategoryId == categoryA).AmountMinor);
    }

    [Fact]
    public async Task GetCategoryTimeSeriesAsync_SoftDeletedCategory_KeepsNullLabel()
    {
        // Arrange
        var (workspaceId, userId) = await TestData.SeedWorkspaceAsync(fixture);
        var accountId = await TestData.SeedAccountAsync(fixture, workspaceId);
        var categoryId = await TestData.SeedCategoryAsync(fixture, workspaceId, TransactionType.Expense);
        const long amount = 40_00;
        await TestData.SeedTransactionAsync(fixture, workspaceId, accountId, categoryId, userId, TransactionType.Expense, amount, occurredAt: _march.AddDays(9));

        await using (var deleteDb = fixture.CreateDbContext())
        {
            var category = await TestData.FindCategoryAsync(deleteDb, fixture.Mapper, categoryId, workspaceId);
            await new CategoryRepository(deleteDb, fixture.Mapper).DeleteAsync(category!, CancellationToken.None);
        }

        // Act
        await using var db = fixture.CreateDbContext();
        var series = await new AnalyticsRepository(db)
            .GetCategoryTimeSeriesAsync(Filter(workspaceId, type: TransactionType.Expense), TimeInterval.Month, CancellationToken.None);

        // Assert
        var point = Assert.Single(series);
        Assert.Equal(categoryId, point.CategoryId);
        Assert.Null(point.Label);
        Assert.Equal(amount, point.AmountMinor);
    }

    private static AnalyticsFilter Filter(
        Guid workspaceId,
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyList<Guid>? accountIds = null,
        TransactionType? type = null,
        IReadOnlyList<Guid>? categoryIds = null) => new(workspaceId, from, to, accountIds, type, categoryIds);
}
