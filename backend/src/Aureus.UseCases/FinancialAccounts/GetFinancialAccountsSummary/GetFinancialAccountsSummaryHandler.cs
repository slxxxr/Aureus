using Aureus.Infrastructure.ExchangeRates;
using Aureus.Persistence.Interfaces;
using MediatR;

namespace Aureus.UseCases.FinancialAccounts.GetFinancialAccountsSummary;

public sealed class GetFinancialAccountsSummaryHandler(
    IFinancialAccountRepository accountRepository,
    IExchangeRateProvider rateProvider) : IRequestHandler<GetFinancialAccountsSummaryQuery, FinancialAccountsSummary?>
{
    private static readonly string[] DisplayCurrencies = ["RUB", "USD", "EUR"];

    public async Task<FinancialAccountsSummary?> Handle(
        GetFinancialAccountsSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var rates = await rateProvider.GetRatesAsync(cancellationToken);
        if (rates is null || DisplayCurrencies.Any(currency => !rates.RublesPerUnit.ContainsKey(currency)))
        {
            return null;
        }

        var accounts = await accountRepository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);
        var balances = accounts.GroupBy(account => account.Currency, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(account => (decimal)account.CurrentBalanceMinor),
                StringComparer.OrdinalIgnoreCase);

        if (balances.Keys.Any(currency => !rates.RublesPerUnit.ContainsKey(currency)))
        {
            return null;
        }

        var totals = DisplayCurrencies.Select(currency =>
        {
            var targetRate = rates.RublesPerUnit[currency];
            var total = balances.GetValueOrDefault(currency);

            foreach (var (sourceCurrency, balance) in balances)
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(sourceCurrency, currency))
                {
                    total += balance * rates.RublesPerUnit[sourceCurrency] / targetRate;
                }
            }

            return new FinancialAccountsSummary.Total(currency, decimal.Round(total, 0, MidpointRounding.AwayFromZero));
        }).ToArray();

        return new FinancialAccountsSummary(rates.RateDate, totals);
    }
}
