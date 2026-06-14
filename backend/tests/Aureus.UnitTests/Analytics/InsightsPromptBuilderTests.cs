using Aureus.Domain.Analytics;
using Aureus.Domain.Transactions;
using Aureus.UseCases.Analytics.GetInsights;

namespace Aureus.UnitTests.Analytics;

public sealed class InsightsPromptBuilderTests
{
    private const string TransactionName = "Пятёрочка";
    private const string CategoryName = "Продукты";
    private const string TopTransactionName = "Ашан";

    private static GetInsightsQuery DefaultQuery(DateOnly? from = null, DateOnly? to = null) => new(
        WorkspaceId: Guid.NewGuid(),
        Question: "Test question",
        From: from ?? new DateOnly(2025, 1, 1),
        To: to ?? new DateOnly(2025, 1, 31));

    private static FinancialContext EmptyContext() => new(
        Summary: [],
        ExpenseCategories: [],
        IncomeCategories: [],
        Transactions: [],
        TimeSeries: [],
        TopNames: []);

    [Fact]
    public void Build_IncludesQuestion()
    {
        // Arrange
        var query = DefaultQuery();

        // Act
        var prompt = InsightsPromptBuilder.Build(query, Tier.Large, TimeInterval.Month, 0, EmptyContext());

        // Assert
        Assert.Contains("Test question", prompt);
        Assert.Contains("Respond in the same language", prompt);
    }

    [Fact]
    public void Build_IncludesPeriodDayCount()
    {
        // Arrange — Jan 1 to Jan 31 = 30 days (half-open)
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 1, 31);
        var expectedDayCount = to.DayNumber - from.DayNumber;
        var query = DefaultQuery(from: from, to: to);

        // Act
        var prompt = InsightsPromptBuilder.Build(query, Tier.Large, TimeInterval.Month, 0, EmptyContext());

        // Assert
        Assert.Contains($"{expectedDayCount} days", prompt);
    }

    [Fact]
    public void Build_FormatsAmountsWithDotNotComma()
    {
        // Arrange — 1500.00 RUB income
        const long incomeMinor = 150000;
        var context = EmptyContext() with
        {
            Summary = [new CurrencySummary("RUB", incomeMinor, 0)],
        };

        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Large, TimeInterval.Month, 0, context);

        // Assert
        Assert.Contains("1500.00", prompt);
        Assert.DoesNotContain("1500,00", prompt);
    }

    [Fact]
    public void Build_SmallTier_ContainsAllTransactionsSection()
    {
        // Arrange
        const long txAmountMinor = 50000;
        const int txCount = 1;
        var txDate = new DateOnly(2025, 1, 10);
        var context = EmptyContext() with
        {
            Transactions =
            [
                new TransactionContext(txDate, TransactionName, CategoryName,
                    TransactionType.Expense, txAmountMinor, "RUB"),
            ],
        };

        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Small, TimeInterval.Day, txCount, context);

        // Assert
        Assert.Contains("All transactions", prompt);
        Assert.Contains(TransactionName, prompt);
        Assert.Contains(txDate.ToString("yyyy-MM-dd"), prompt);
    }

    [Fact]
    public void Build_SmallTier_DoesNotContainTimeSeriesSection()
    {
        // Arrange
        const int txCount = 1;

        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Small, TimeInterval.Day, txCount, EmptyContext());

        // Assert
        Assert.DoesNotContain("Daily income/expense totals", prompt);
        Assert.DoesNotContain("Weekly income/expense totals", prompt);
        Assert.DoesNotContain("Monthly income/expense totals", prompt);
    }

    [Fact]
    public void Build_MediumTier_ContainsCategoryMatrixWithTop3()
    {
        // Arrange
        const long topTxAmountMinor = 60000;
        const long otherTxAmountMinor = 40000;
        const int count = 200;
        var context = EmptyContext() with
        {
            Transactions =
            [
                new TransactionContext(new DateOnly(2025, 1, 7), TopTransactionName, CategoryName, TransactionType.Expense, topTxAmountMinor, "RUB"),
                new TransactionContext(new DateOnly(2025, 1, 8), TransactionName, CategoryName, TransactionType.Expense, otherTxAmountMinor, "RUB"),
            ],
        };

        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Medium, TimeInterval.Week, count, context);

        // Assert
        Assert.Contains("Category breakdown by week", prompt);
        Assert.Contains(CategoryName, prompt);
        Assert.Contains(TopTransactionName, prompt);
    }

    [Fact]
    public void Build_LargeTier_ContainsTimeSeriesNotTransactionList()
    {
        // Arrange
        const long timeSeriesIncomeMinor = 500000;
        const long timeSeriesExpenseMinor = 300000;
        const int count = 1000;
        var context = EmptyContext() with
        {
            TimeSeries = [new TimeSeriesPoint(new DateOnly(2025, 1, 1), "RUB", timeSeriesIncomeMinor, timeSeriesExpenseMinor)],
        };

        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Large, TimeInterval.Month, count, context);

        // Assert
        Assert.Contains("Monthly income/expense totals", prompt);
        Assert.DoesNotContain("All transactions", prompt);
    }

    [Fact]
    public void Build_LargeTier_ContainsTopNamesSection()
    {
        // Arrange
        const long topTransactionAmountMinor = 150000;
        const int count = 1000;
        var context = EmptyContext() with
        {
            TopNames = [new BreakdownItem("key", TransactionName, "RUB", topTransactionAmountMinor)],
        };

        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Large, TimeInterval.Month, count, context);

        // Assert
        Assert.Contains($"Top {context.TopNames.Count} transaction names", prompt);
        Assert.Contains(TransactionName, prompt);
    }

    [Fact]
    public void Build_EmptySummary_WritesNoTransactionsMessage()
    {
        // Act
        var prompt = InsightsPromptBuilder.Build(DefaultQuery(), Tier.Large, TimeInterval.Month, 0, EmptyContext());

        // Assert
        Assert.Contains("No transactions in this period", prompt);
    }
}
