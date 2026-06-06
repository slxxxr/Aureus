using Aureus.Domain.FinancialAccounts;
using Aureus.UseCases.Common.Persistence;
using Moq;

namespace Aureus.UnitTests.Mocks;

public sealed class FinancialAccountRepositoryMock
{
    private readonly Mock<IFinancialAccountRepository> _mock = new();

    public IFinancialAccountRepository Object => _mock.Object;

    public FinancialAccount? SavedAccount { get; private set; }

    public FinancialAccount? UpdatedAccount { get; private set; }

    public FinancialAccount? DeletedAccount { get; private set; }

    public FinancialAccountRepositoryMock WithAccounts(Guid workspaceId, IReadOnlyList<FinancialAccount> accounts)
    {
        _mock
            .Setup(r => r.GetByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        return this;
    }

    public FinancialAccountRepositoryMock WithAccount(Guid id, Guid workspaceId, FinancialAccount account)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        return this;
    }

    public FinancialAccountRepositoryMock WithNoAccount(Guid id, Guid workspaceId)
    {
        _mock
            .Setup(r => r.FindByIdAsync(id, workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinancialAccount?)null);

        return this;
    }

    public FinancialAccountRepositoryMock CapturingAdd()
    {
        _mock
            .Setup(r => r.AddAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()))
            .Callback<FinancialAccount, CancellationToken>((account, _) => SavedAccount = account)
            .Returns(Task.CompletedTask);

        return this;
    }

    public FinancialAccountRepositoryMock CapturingUpdate()
    {
        _mock
            .Setup(r => r.UpdateAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()))
            .Callback<FinancialAccount, CancellationToken>((account, _) => UpdatedAccount = account)
            .Returns(Task.CompletedTask);

        return this;
    }

    public FinancialAccountRepositoryMock CapturingDelete()
    {
        _mock
            .Setup(r => r.DeleteAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()))
            .Callback<FinancialAccount, CancellationToken>((account, _) => DeletedAccount = account)
            .Returns(Task.CompletedTask);

        return this;
    }

    public void VerifyAddCalledOnce() =>
        _mock.Verify(r => r.AddAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyUpdateCalledOnce() =>
        _mock.Verify(r => r.UpdateAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteCalledOnce() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyDeleteNotCalled() =>
        _mock.Verify(r => r.DeleteAsync(It.IsAny<FinancialAccount>(), It.IsAny<CancellationToken>()), Times.Never);
}
