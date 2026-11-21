using Aureus.Domain.FinancialAccounts;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;

namespace Aureus.UnitTests.FinancialAccounts;

public sealed class UpdateFinancialAccountHandlerTests
{
    private static FinancialAccount ExistingAccount(
        long initialBalanceMinor = 1000_00,
        long currentBalanceMinor = 800_00,
        string name = "Cash")
    {
        return new FinancialAccount
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            Name = name,
            Currency = "RUB",
            InitialBalanceMinor = initialBalanceMinor,
            CurrentBalanceMinor = currentBalanceMinor,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsFinancialAccountException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var repository = new FinancialAccountRepositoryMock().WithNoAccount(accountId, workspaceId);
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var exception = await Assert.ThrowsAsync<FinancialAccountException>(() =>
            handler.Handle(
                new UpdateFinancialAccountCommand(accountId, workspaceId, "New name", null),
                CancellationToken.None));

        // Assert
        Assert.Equal(FinancialAccountErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_NameProvided_UpdatesNameLeavesBalancesUnchanged()
    {
        // Arrange
        var account = ExistingAccount(1000_00, 800_00);
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, "  Wallet  ", null),
            CancellationToken.None);

        // Assert
        Assert.Equal("Wallet", result.Name);
        Assert.Equal(account.InitialBalanceMinor, result.InitialBalanceMinor);
        Assert.Equal(account.CurrentBalanceMinor, result.CurrentBalanceMinor);
    }

    [Theory]
    [InlineData(1000_00, 800_00, 1500_00, 1300_00)] // increase initial by 500 → current increases by 500
    [InlineData(1000_00, 1200_00, 500_00, 700_00)] // decrease initial by 500 → current decreases by 500
    [InlineData(1000_00, 800_00, 1000_00, 800_00)] // same initial → current unchanged
    [InlineData(0, 0, 500_00, 500_00)] // from zero
    [InlineData(1000_00, 800_00, 0, -200_00)] // decrease to zero → current goes negative
    public async Task Handle_InitialBalanceProvided_RecalculatesCurrentBalanceByDelta(
        long oldInitial, long oldCurrent, long newInitial, long expectedCurrent)
    {
        // Arrange
        var account = ExistingAccount(oldInitial, oldCurrent);
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, null, newInitial),
            CancellationToken.None);

        // Assert
        Assert.Equal(newInitial, result.InitialBalanceMinor);
        Assert.Equal(expectedCurrent, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_BothNameAndBalance_UpdatesBoth()
    {
        // Arrange
        var account = ExistingAccount(1000_00, 900_00, "Cash");
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var command = new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, "Card", 2000_00);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.InitialBalanceMinor, result.InitialBalanceMinor);
        Assert.Equal(1900_00, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_LeavesNameAndBalancesUnchanged()
    {
        // Arrange
        var account = ExistingAccount(1000_00, 800_00, "Cash");
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, null, null),
            CancellationToken.None);

        // Assert
        Assert.Equal(account.Name, result.Name);
        Assert.Equal(account.InitialBalanceMinor, result.InitialBalanceMinor);
        Assert.Equal(account.CurrentBalanceMinor, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_NameProvided_NormalizesWhitespace()
    {
        // Arrange
        var account = ExistingAccount(name: "Cash");
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, "  Savings  ", null),
            CancellationToken.None);

        // Assert
        Assert.Equal("Savings", result.Name);
    }
}