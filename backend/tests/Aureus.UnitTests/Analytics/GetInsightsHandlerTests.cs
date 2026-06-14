using Aureus.Domain.Analytics;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Analytics.GetInsights;

namespace Aureus.UnitTests.Analytics;

public sealed class GetInsightsHandlerTests
{
    private static GetInsightsQuery DefaultQuery() => new(
        WorkspaceId: Guid.NewGuid(),
        Question: "What did I spend?",
        From: new DateOnly(2025, 1, 1),
        To: new DateOnly(2025, 1, 31));

    private static GetInsightsHandler BuildHandler(
        AnalyticsRepositoryMock? repository = null,
        WorkspaceDailyUsageRepositoryMock? dailyUsage = null,
        LlmClientMock? llm = null)
    {
        repository ??= new AnalyticsRepositoryMock().WithCount(10);
        dailyUsage ??= new WorkspaceDailyUsageRepositoryMock().WithCount(1);
        llm ??= new LlmClientMock().WithAnswer("ok");
        return new GetInsightsHandler(repository.Object, dailyUsage.Object, llm.Object);
    }

    [Fact]
    public async Task Handle_SmallTierCount_FetchesTransactionsOnly()
    {
        // Arrange
        const int countInSmallTier = 50;
        var repository = new AnalyticsRepositoryMock().WithCount(countInSmallTier);
        var handler = BuildHandler(repository: repository);

        // Act
        await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        repository.VerifyTransactionsFetched();
        repository.VerifyTimeSeriesNotFetched();
        repository.VerifyNameBreakdownNotFetched();
    }

    [Fact]
    public async Task Handle_SmallTierBoundary_FetchesTransactionsOnly()
    {
        // Arrange
        const int countAtSmallTierEdge = 100;
        var repository = new AnalyticsRepositoryMock().WithCount(countAtSmallTierEdge);
        var handler = BuildHandler(repository: repository);

        // Act
        await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        repository.VerifyTransactionsFetched();
        repository.VerifyTimeSeriesNotFetched();
    }

    [Fact]
    public async Task Handle_MediumTierCount_FetchesBothTransactionsAndTimeSeries()
    {
        // Arrange
        const int countInMediumTier = 250;
        var repository = new AnalyticsRepositoryMock().WithCount(countInMediumTier);
        var handler = BuildHandler(repository: repository);

        // Act
        await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        repository.VerifyTransactionsFetched();
        repository.VerifyTimeSeriesFetched();
        repository.VerifyNameBreakdownNotFetched();
    }

    [Fact]
    public async Task Handle_MediumTierBoundary_FetchesTransactionsAndTimeSeries()
    {
        // Arrange
        const int countAtMediumTierEdge = 500;
        var repository = new AnalyticsRepositoryMock().WithCount(countAtMediumTierEdge);
        var handler = BuildHandler(repository: repository);

        // Act
        await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        repository.VerifyTransactionsFetched();
        repository.VerifyTimeSeriesFetched();
    }

    [Fact]
    public async Task Handle_LargeTierCount_FetchesTimeSeriesAndNamesNotTransactions()
    {
        // Arrange
        const int countInLargeTier = 501;
        var repository = new AnalyticsRepositoryMock().WithCount(countInLargeTier);
        var handler = BuildHandler(repository: repository);

        // Act
        await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        repository.VerifyTransactionsNotFetched();
        repository.VerifyTimeSeriesFetched();
        repository.VerifyNameBreakdownFetched();
    }

    [Fact]
    public async Task Handle_ReturnsLlmAnswer()
    {
        // Arrange
        const string expectedAnswer = "Вы потратили 5 000 рублей.";
        var llm = new LlmClientMock().WithAnswer(expectedAnswer);
        var handler = BuildHandler(llm: llm);

        // Act
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(expectedAnswer, result);
    }

    [Fact]
    public async Task Handle_DailyQuotaExceeded_ThrowsAnalyticsException()
    {
        // Arrange
        var dailyUsage = new WorkspaceDailyUsageRepositoryMock().WithCount(21);
        var handler = BuildHandler(dailyUsage: dailyUsage);

        // Act
        var exception = await Assert.ThrowsAsync<AnalyticsException>(() =>
            handler.Handle(DefaultQuery(), CancellationToken.None));

        // Assert
        Assert.Equal(AnalyticsErrorCode.DailyQuotaExceeded, exception.Code);
    }

    [Fact]
    public async Task Handle_LlmRateLimited_ThrowsAnalyticsException()
    {
        // Arrange
        var llm = new LlmClientMock().ThrowingRateLimit();
        var handler = BuildHandler(llm: llm);

        // Act
        var exception = await Assert.ThrowsAsync<AnalyticsException>(() =>
            handler.Handle(DefaultQuery(), CancellationToken.None));

        // Assert
        Assert.Equal(AnalyticsErrorCode.LlmTemporarilyUnavailable, exception.Code);
    }
}
