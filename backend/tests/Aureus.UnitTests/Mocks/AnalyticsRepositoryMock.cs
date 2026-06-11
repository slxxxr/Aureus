using Aureus.Domain.Analytics;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class AnalyticsRepositoryMock
{
    private readonly Mock<IAnalyticsRepository> _mock = new();

    public IAnalyticsRepository Object => _mock.Object;

    public AnalyticsRepositoryMock()
    {
        _mock.Setup(r => r.GetSummaryAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mock.Setup(r => r.GetBreakdownAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<BreakdownDimension>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mock.Setup(r => r.GetTimeSeriesAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<TimeInterval>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mock.Setup(r => r.GetCategoryTimeSeriesAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<TimeInterval>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mock.Setup(r => r.GetTransactionCountAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mock.Setup(r => r.GetTransactionsForContextAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    public AnalyticsRepositoryMock WithCount(int count)
    {
        _mock.Setup(r => r.GetTransactionCountAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);
        return this;
    }

    public void VerifyTransactionsFetched() =>
        _mock.Verify(r => r.GetTransactionsForContextAsync(
            It.IsAny<AnalyticsFilter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyTransactionsNotFetched() =>
        _mock.Verify(r => r.GetTransactionsForContextAsync(
            It.IsAny<AnalyticsFilter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    public void VerifyTimeSeriesFetched() =>
        _mock.Verify(r => r.GetTimeSeriesAsync(
            It.IsAny<AnalyticsFilter>(), It.IsAny<TimeInterval>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

    public void VerifyTimeSeriesNotFetched() =>
        _mock.Verify(r => r.GetTimeSeriesAsync(
            It.IsAny<AnalyticsFilter>(), It.IsAny<TimeInterval>(), It.IsAny<CancellationToken>()), Times.Never);

    public void VerifyNameBreakdownFetched() =>
        _mock.Verify(r => r.GetBreakdownAsync(
            It.IsAny<AnalyticsFilter>(), BreakdownDimension.Name, It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyNameBreakdownNotFetched() =>
        _mock.Verify(r => r.GetBreakdownAsync(
            It.IsAny<AnalyticsFilter>(), BreakdownDimension.Name, It.IsAny<CancellationToken>()), Times.Never);
}
