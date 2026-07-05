using Aureus.Domain.Transactions;
using Aureus.Domain.Transfers;
using Aureus.Persistence.Interfaces;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class ImportRepositoryMock
{
    private readonly Mock<IImportRepository> _mock = new();

    public IImportRepository Object => _mock.Object;

    public IReadOnlyList<Transaction>? AddedTransactions { get; private set; }
    public IReadOnlyDictionary<Guid, long>? AddedTransactionDeltas { get; private set; }
    public IReadOnlyList<Transfer>? AddedTransfers { get; private set; }
    public IReadOnlyDictionary<Guid, long>? AddedTransferDeltas { get; private set; }

    public ImportRepositoryMock CapturingAddBulk()
    {
        _mock
            .Setup(r => r.AddBulkAsync(
                It.IsAny<IReadOnlyList<Transaction>>(),
                It.IsAny<IReadOnlyDictionary<Guid, long>>(),
                It.IsAny<IReadOnlyList<Transfer>>(),
                It.IsAny<IReadOnlyDictionary<Guid, long>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Transaction>, IReadOnlyDictionary<Guid, long>, IReadOnlyList<Transfer>, IReadOnlyDictionary<Guid, long>, CancellationToken>(
                (transactions, transactionDeltas, transfers, transferDeltas, _) =>
                {
                    AddedTransactions = transactions;
                    AddedTransactionDeltas = transactionDeltas;
                    AddedTransfers = transfers;
                    AddedTransferDeltas = transferDeltas;
                })
            .Returns(Task.CompletedTask);

        return this;
    }
}
