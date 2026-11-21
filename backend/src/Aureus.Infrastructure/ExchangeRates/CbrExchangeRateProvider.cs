using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Aureus.Infrastructure.ExchangeRates;

public sealed class CbrExchangeRateProvider(
    HttpClient client,
    TimeProvider timeProvider,
    ILogger<CbrExchangeRateProvider> logger) : IExchangeRateProvider
{
    private static readonly TimeZoneInfo MoscowTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    static CbrExchangeRateProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<ExchangeRates?> GetRatesAsync(CancellationToken cancellationToken)
    {
        var moscowNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), MoscowTime);
        var requestedDate = DateOnly.FromDateTime(moscowNow.DateTime);
        var dateParameter = requestedDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        try
        {
            using var response = await client.GetAsync($"scripts/XML_daily.asp?date_req={dateParameter}",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
            var root = document.Element("ValCurs");
            if (root is null)
            {
                return null;
            }

            var dateText = root.Attribute("Date")?.Value;
            var hasRateDate = DateOnly.TryParseExact(dateText, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var rateDate);
            if (!hasRateDate || rateDate > requestedDate)
            {
                return null;
            }

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["RUB"] = 1m };

            foreach (var currency in root.Elements("Valute"))
            {
                var code = currency.Element("CharCode")?.Value;
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var nominalText = currency.Element("Nominal")?.Value;
                var valueText = currency.Element("Value")?.Value;
                var hasNominal = int.TryParse(nominalText, CultureInfo.InvariantCulture, out var nominal) && nominal > 0;
                var hasValue = decimal.TryParse(valueText, RussianCulture, out var value) && value > 0;

                if (hasNominal && hasValue)
                {
                    rates.TryAdd(code, value / nominal);
                }
            }

            return new ExchangeRates(rateDate, rates);
        }
        catch (Exception exception) when (exception is HttpRequestException or XmlException ||
                                          exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not load exchange rates from the Bank of Russia");
            return null;
        }
    }
}
