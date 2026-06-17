using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class WorkspaceDailyUsageRepositoryMock
{
    private readonly Mock<IWorkspaceDailyUsageRepository> _mock = new();

    public IWorkspaceDailyUsageRepository Object => _mock.Object;

    public WorkspaceDailyUsageRepositoryMock WithCount(int count)
    {
        _mock.Setup(r => r.IncrementAndGetAsync(
                It.IsAny<Guid>(),
                It.IsAny<DailyUsageFeature>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);
        return this;
    }

    public WorkspaceDailyUsageRepositoryMock WithCurrentCount(int count)
    {
        _mock.Setup(r => r.GetCountAsync(
                It.IsAny<Guid>(),
                It.IsAny<DailyUsageFeature>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);
        return this;
    }

    public void VerifyIncrementCalledOnce()
    {
        _mock.Verify(r => r.IncrementAndGetAsync(
            It.IsAny<Guid>(), It.IsAny<DailyUsageFeature>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public void VerifyIncrementNotCalled()
    {
        _mock.Verify(r => r.IncrementAndGetAsync(
            It.IsAny<Guid>(), It.IsAny<DailyUsageFeature>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
