using Aureus.UnitTests.Mocks;
using Aureus.UseCases.Analytics.GetInsights;

namespace Aureus.UnitTests.Analytics;

public sealed class GetInsightsHandlerTests
{
    private static GetInsightsQuery DefaultQuery() => new(
        WorkspaceId: Guid.NewGuid(),
        Question: "What did I spend?",
        From: new DateOnly(2025, 1, 1),
        To: new DateOnly(2025, 1, 31),
        Language: "Russian");

    [Fact]
    public async Task Handle_SmallTierCount_FetchesTransactionsOnly()
    {
        // Arrange
        const int countInSmallTier = 50;
        var repository = new AnalyticsRepositoryMock().WithCount(countInSmallTier);
        var llm = new LlmClientMock().WithAnswer("ok");
        var handler = new GetInsightsHandler(repository.Object, llm.Object);

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
        // Arrange — exactly at the upper edge of small tier
        const int countAtSmallTierEdge = 100;
        var repository = new AnalyticsRepositoryMock().WithCount(countAtSmallTierEdge);
        var llm = new LlmClientMock().WithAnswer("ok");
        var handler = new GetInsightsHandler(repository.Object, llm.Object);

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
        var llm = new LlmClientMock().WithAnswer("ok");
        var handler = new GetInsightsHandler(repository.Object, llm.Object);

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
        // Arrange — exactly at the upper edge of medium tier
        const int countAtMediumTierEdge = 500;
        var repository = new AnalyticsRepositoryMock().WithCount(countAtMediumTierEdge);
        var llm = new LlmClientMock().WithAnswer("ok");
        var handler = new GetInsightsHandler(repository.Object, llm.Object);

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
        var llm = new LlmClientMock().WithAnswer("ok");
        var handler = new GetInsightsHandler(repository.Object, llm.Object);

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
        const int anySmallCount = 10;
        var repository = new AnalyticsRepositoryMock().WithCount(anySmallCount);
        var llm = new LlmClientMock().WithAnswer(expectedAnswer);
        var handler = new GetInsightsHandler(repository.Object, llm.Object);

        // Act
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(expectedAnswer, result);
    }
}
