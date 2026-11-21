namespace Aureus.Infrastructure.ExchangeRates;

public sealed record ExchangeRates(DateOnly RateDate, IReadOnlyDictionary<string, decimal> RublesPerUnit);
