using Aureus.Domain.FinancialAccounts;
using Aureus.Infrastructure.ExchangeRates;
using Aureus.UnitTests.Mocks;
using Aureus.UseCases.FinancialAccounts.GetFinancialAccountsSummary;
using Moq;

namespace Aureus.UnitTests.FinancialAccounts;

public sealed class GetFinancialAccountsSummaryHandlerTests
{
    [Fact]
    public async Task Handle_CalculatesTotalsFromCurrentBalancesInDisplayOrder()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var rateDate = new DateOnly(2026, 11, 21);
        var repository = new FinancialAccountRepositoryMock().WithAccounts(workspaceId,
        [
            new FinancialAccount { Currency = "RUB", CurrentBalanceMinor = 1_000_000 },
            new FinancialAccount { Currency = "USD", CurrentBalanceMinor = 11_000 },
            new FinancialAccount { Currency = "USD", CurrentBalanceMinor = -1_000 },
            new FinancialAccount { Currency = "EUR", CurrentBalanceMinor = 5_000 }
        ]);
        var rateProvider = new Mock<IExchangeRateProvider>();
        rateProvider.Setup(provider => provider.GetRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRates(rateDate, new Dictionary<string, decimal>
            {
                ["RUB"] = 1m,
                ["USD"] = 90m,
                ["EUR"] = 100m
            }));
        var handler = new GetFinancialAccountsSummaryHandler(repository.Object, rateProvider.Object);

        // Act
        var result = await handler.Handle(new GetFinancialAccountsSummaryQuery(workspaceId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(rateDate, result.RateDate);
        Assert.Equal(["RUB", "USD", "EUR"], result.Totals.Select(total => total.Currency));
        Assert.Equal([2_400_000m, 26_667m, 24_000m], result.Totals.Select(total => total.AmountMinor));
        rateProvider.Verify(provider => provider.GetRatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoundsAfterAddingAllConvertedBalances()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var repository = new FinancialAccountRepositoryMock().WithAccounts(workspaceId,
        [
            new FinancialAccount { Currency = "RUB", CurrentBalanceMinor = 1 },
            new FinancialAccount { Currency = "EUR", CurrentBalanceMinor = 1 }
        ]);
        var rateProvider = new Mock<IExchangeRateProvider>();
        rateProvider.Setup(provider => provider.GetRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRates(new DateOnly(2026, 11, 21), new Dictionary<string, decimal>
            {
                ["RUB"] = 1m,
                ["USD"] = 4m,
                ["EUR"] = 1m
            }));
        var handler = new GetFinancialAccountsSummaryHandler(repository.Object, rateProvider.Object);

        // Act
        var result = await handler.Handle(new GetFinancialAccountsSummaryQuery(workspaceId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1m, result.Totals.Single(total => total.Currency == "USD").AmountMinor);
    }

    [Fact]
    public async Task Handle_WhenAnAccountCurrencyHasNoRate_ReturnsNoSummary()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var repository = new FinancialAccountRepositoryMock().WithAccounts(workspaceId,
            [new FinancialAccount { Currency = "XYZ", CurrentBalanceMinor = 1_000 }]);
        var rateProvider = new Mock<IExchangeRateProvider>();
        rateProvider.Setup(provider => provider.GetRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRates(new DateOnly(2026, 11, 21), new Dictionary<string, decimal>
            {
                ["RUB"] = 1m,
                ["USD"] = 90m,
                ["EUR"] = 100m
            }));
        var handler = new GetFinancialAccountsSummaryHandler(repository.Object, rateProvider.Object);

        // Act
        var result = await handler.Handle(new GetFinancialAccountsSummaryQuery(workspaceId), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
