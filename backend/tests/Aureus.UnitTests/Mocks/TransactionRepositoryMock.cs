using Aureus.Domain.Transactions;
using Aureus.UseCases.Common.Persistence;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class TransactionRepositoryMock
{
    private readonly Mock<ITransactionRepository> _mock = new();

    public ITransactionRepository Object => _mock.Object;

    public Transaction? SavedTransaction { get; private set; }
    public long? SavedBalanceDelta { get; private set; }

    public Transaction? UpdatedTransaction { get; private set; }
    public long? UpdatedBalanceDelta { get; private set; }

    public Transaction? DeletedTransaction { get; private set; }
    public long? DeletedBalanceDelta { get; private set; }

    public TransactionRepositoryMock WithTransaction(Guid id, Guid workspaceId, Transaction transaction)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        return this;
    }

    public TransactionRepositoryMock WithNoTransaction(Guid id, Guid workspaceId)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        return this;
    }

    public TransactionRepositoryMock CapturingAdd()
    {
        _mock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, long, CancellationToken>((t, delta, _) =>
            {
                SavedTransaction = t;
                SavedBalanceDelta = delta;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public TransactionRepositoryMock CapturingUpdate()
    {
        _mock
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, long, CancellationToken>((t, delta, _) =>
            {
                UpdatedTransaction = t;
                UpdatedBalanceDelta = delta;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public TransactionRepositoryMock CapturingDelete()
    {
        _mock
            .Setup(r => r.DeleteAsync(It.IsAny<Transaction>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, long, CancellationToken>((t, delta, _) =>
            {
                DeletedTransaction = t;
                DeletedBalanceDelta = delta;
            })
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyDeleteCalledOnce() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Transaction>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteNotCalled() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<Transaction>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
}
