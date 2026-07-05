using Aureus.Domain.Analytics;
using Aureus.Domain.Transfers;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class TransferRepositoryMock
{
    private readonly Mock<ITransferRepository> _mock = new();

    public ITransferRepository Object => _mock.Object;

    public Transfer? SavedTransfer { get; private set; }

    public Transfer? UpdatedTransfer { get; private set; }
    public IReadOnlyDictionary<Guid, long>? UpdatedAccountDeltas { get; private set; }

    public Transfer? DeletedTransfer { get; private set; }

    public TransferRepositoryMock WithFilterResult(IReadOnlyList<Transfer> transfers)
    {
        _mock
            .Setup(r => r.GetByFilterAsync(It.IsAny<AnalyticsFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfers);

        return this;
    }

    public TransferRepositoryMock WithTransfer(Guid id, Guid workspaceId, Transfer transfer)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        return this;
    }

    public TransferRepositoryMock WithNoTransfer(Guid id, Guid workspaceId)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transfer?)null);

        return this;
    }

    public TransferRepositoryMock CapturingAdd()
    {
        _mock
            .Setup(r => r.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()))
            .Callback<Transfer, CancellationToken>((t, _) => SavedTransfer = t)
            .Returns(Task.CompletedTask);

        return this;
    }

    public TransferRepositoryMock CapturingUpdate()
    {
        _mock
            .Setup(r => r.UpdateAsync(
                It.IsAny<Transfer>(),
                It.IsAny<IReadOnlyDictionary<Guid, long>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Transfer, IReadOnlyDictionary<Guid, long>, CancellationToken>((t, deltas, _) =>
            {
                UpdatedTransfer = t;
                UpdatedAccountDeltas = deltas;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public TransferRepositoryMock CapturingDelete()
    {
        _mock
            .Setup(r => r.DeleteAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()))
            .Callback<Transfer, CancellationToken>((t, _) => DeletedTransfer = t)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyDeleteCalledOnce() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteNotCalled() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);

    public void VerifyUpdateNotCalled() =>
        _mock.Verify(r => r.UpdateAsync(
            It.IsAny<Transfer>(), It.IsAny<IReadOnlyDictionary<Guid, long>>(), It.IsAny<CancellationToken>()), Times.Never);
}
