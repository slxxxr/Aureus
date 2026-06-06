using Aureus.Domain.FinancialAccounts;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.FinancialAccounts.DeleteFinancialAccount;

namespace Aureus.UnitTests.FinancialAccounts;

public sealed class DeleteFinancialAccountHandlerTests
{
    [Fact]
    public async Task Handle_AccountNotFound_ThrowsFinancialAccountException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var repository = new FinancialAccountRepositoryMock().WithNoAccount(accountId, workspaceId);
        var handler = new DeleteFinancialAccountHandler(repository.Object);

        // Act
        var exception = await Assert.ThrowsAsync<FinancialAccountException>(() =>
            handler.Handle(
                new DeleteFinancialAccountCommand(accountId, workspaceId),
                CancellationToken.None));

        // Assert
        Assert.Equal(FinancialAccountErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_AccountNotFound_DoesNotCallDelete()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var repository = new FinancialAccountRepositoryMock().WithNoAccount(accountId, workspaceId);
        var handler = new DeleteFinancialAccountHandler(repository.Object);

        // Act
        await Assert.ThrowsAsync<FinancialAccountException>(() =>
            handler.Handle(
                new DeleteFinancialAccountCommand(accountId, workspaceId),
                CancellationToken.None));

        // Assert
        repository.VerifyDeleteNotCalled();
    }

    [Fact]
    public async Task Handle_AccountExists_CallsDeleteWithCorrectAccount()
    {
        // Arrange
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            Name = "Cash",
            Currency = "RUB",
            InitialBalanceMinor = 1000_00,
            CurrentBalanceMinor = 1000_00,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingDelete();
        var handler = new DeleteFinancialAccountHandler(repository.Object);

        // Act
        await handler.Handle(
            new DeleteFinancialAccountCommand(account.Id, account.WorkspaceId),
            CancellationToken.None);

        // Assert
        repository.VerifyDeleteCalledOnce();
        Assert.NotNull(repository.DeletedAccount);
        Assert.Equal(account.Id, repository.DeletedAccount.Id);
    }
}
