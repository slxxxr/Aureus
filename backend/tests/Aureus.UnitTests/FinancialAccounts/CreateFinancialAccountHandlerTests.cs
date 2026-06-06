using Aureus.UnitTests.Mocks;
using Aureus.UseCases.FinancialAccounts.CreateFinancialAccount;

namespace Aureus.UnitTests.FinancialAccounts;

public sealed class CreateFinancialAccountHandlerTests
{
    private static CreateFinancialAccountCommand DefaultCommand(
        string name = "Cash",
        string currency = "RUB",
        long initialBalanceMinor = 100_00) =>
        new(WorkspaceId: Guid.NewGuid(), Name: name, Currency: currency, InitialBalanceMinor: initialBalanceMinor);

    [Fact]
    public async Task Handle_ValidCommand_SetsCurrentBalanceEqualToInitialBalance()
    {
        // Arrange
        var repository = new FinancialAccountRepositoryMock().CapturingAdd();
        var handler = new CreateFinancialAccountHandler(repository.Object);
        var command = DefaultCommand(initialBalanceMinor: 5000_00);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.InitialBalanceMinor, result.InitialBalanceMinor);
        Assert.Equal(command.InitialBalanceMinor, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesWhitespace()
    {
        // Arrange
        var repository = new FinancialAccountRepositoryMock().CapturingAdd();
        var handler = new CreateFinancialAccountHandler(repository.Object);
        var command = DefaultCommand(name: "  Cash  ");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Cash", result.Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCorrectWorkspaceAndCurrency()
    {
        // Arrange
        var repository = new FinancialAccountRepositoryMock().CapturingAdd();
        var handler = new CreateFinancialAccountHandler(repository.Object);
        var command = DefaultCommand(currency: "USD");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.WorkspaceId, result.WorkspaceId);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task Handle_ValidCommand_AssignsNonEmptyId()
    {
        // Arrange
        var repository = new FinancialAccountRepositoryMock().CapturingAdd();
        var handler = new CreateFinancialAccountHandler(repository.Object);

        // Act
        var result = await handler.Handle(DefaultCommand(), CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_ZeroInitialBalance_SetsCurrentBalanceToZero()
    {
        // Arrange
        var repository = new FinancialAccountRepositoryMock().CapturingAdd();
        var handler = new CreateFinancialAccountHandler(repository.Object);
        var command = DefaultCommand(initialBalanceMinor: 0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.InitialBalanceMinor);
        Assert.Equal(0, result.CurrentBalanceMinor);
    }

    [Fact]
    public async Task Handle_NegativeInitialBalance_SetsCurrentBalanceToSameNegativeValue()
    {
        // Arrange
        var repository = new FinancialAccountRepositoryMock().CapturingAdd();
        var handler = new CreateFinancialAccountHandler(repository.Object);
        var command = DefaultCommand(initialBalanceMinor: -500_00);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(-500_00, result.CurrentBalanceMinor);
    }
}
