using Aureus.Domain.FinancialAccounts;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.FinancialAccounts.UpdateFinancialAccount;

namespace Aureus.UnitTests.FinancialAccounts;

public sealed class UpdateFinancialAccountHandlerTests
{
    private static FinancialAccount ExistingAccount(
        long initialBalanceMinor = 1000_00,
        long currentBalanceMinor = 800_00,
        string name = "Cash") =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            Name = name,
            Currency = "RUB",
            InitialBalanceMinor = initialBalanceMinor,
            CurrentBalanceMinor = currentBalanceMinor,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

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
                new UpdateFinancialAccountCommand(accountId, workspaceId, Name: "New name", InitialBalanceMinor: null),
                CancellationToken.None));

        // Assert
        Assert.Equal(FinancialAccountErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public async Task Handle_NameProvided_UpdatesNameLeavesBalancesUnchanged()
    {
        // Arrange
        var account = ExistingAccount(initialBalanceMinor: 1000_00, currentBalanceMinor: 800_00);
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, Name: "  Wallet  ", InitialBalanceMinor: null),
            CancellationToken.None);

        // Assert
        Assert.Equal("Wallet", result.Name);
        Assert.Equal(1000_00, result.InitialBalanceMinor);
        Assert.Equal(800_00, result.CurrentBalanceMinor);
    }

    [Theory]
    [InlineData(1000_00, 800_00, 1500_00, 1300_00)]  // increase initial by 500 → current increases by 500
    [InlineData(1000_00, 1200_00, 500_00,  700_00)]  // decrease initial by 500 → current decreases by 500
    [InlineData(1000_00, 800_00,  1000_00, 800_00)]  // same initial → current unchanged
    [InlineData(0,       0,       500_00,  500_00)]  // from zero
    [InlineData(1000_00, 800_00,  0,       -200_00)] // decrease to zero → current goes negative
    public async Task Handle_InitialBalanceProvided_RecalculatesCurrentBalanceByDelta(
        long oldInitial, long oldCurrent, long newInitial, long expectedCurrent)
    {
        // Arrange
        var account = ExistingAccount(initialBalanceMinor: oldInitial, currentBalanceMinor: oldCurrent);
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, Name: null, InitialBalanceMinor: newInitial),
            CancellationToken.None);

        // Assert
        Assert.Equal(newInitial, result.InitialBalanceMinor);
        Assert.Equal(expectedCurrent, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_BothNameAndBalance_UpdatesBoth()
    {
        // Arrange
        var account = ExistingAccount(initialBalanceMinor: 1000_00, currentBalanceMinor: 900_00, name: "Cash");
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, Name: "Card", InitialBalanceMinor: 2000_00),
            CancellationToken.None);

        // Assert
        Assert.Equal("Card", result.Name);
        Assert.Equal(2000_00, result.InitialBalanceMinor);
        Assert.Equal(1900_00, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_LeavesNameAndBalancesUnchanged()
    {
        // Arrange
        var account = ExistingAccount(initialBalanceMinor: 1000_00, currentBalanceMinor: 800_00, name: "Cash");
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, Name: null, InitialBalanceMinor: null),
            CancellationToken.None);

        // Assert
        Assert.Equal("Cash", result.Name);
        Assert.Equal(1000_00, result.InitialBalanceMinor);
        Assert.Equal(800_00, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_NameProvided_TrimsWhitespace()
    {
        // Arrange
        var account = ExistingAccount(name: "Cash");
        var repository = new FinancialAccountRepositoryMock()
            .WithAccount(account.Id, account.WorkspaceId, account)
            .CapturingUpdate();
        var handler = new UpdateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, account.WorkspaceId, Name: "  Savings  ", InitialBalanceMinor: null),
            CancellationToken.None);

        // Assert
        Assert.Equal("Savings", result.Name);
    }
}
