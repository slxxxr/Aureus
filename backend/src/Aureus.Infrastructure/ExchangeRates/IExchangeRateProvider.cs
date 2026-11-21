namespace Aureus.Infrastructure.ExchangeRates;

public interface IExchangeRateProvider
{
    Task<ExchangeRates?> GetRatesAsync(CancellationToken cancellationToken);
}
