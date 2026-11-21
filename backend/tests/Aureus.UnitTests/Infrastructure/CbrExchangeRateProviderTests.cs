using System.Net;
using System.Text;
using Aureus.Infrastructure.ExchangeRates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aureus.UnitTests.Infrastructure;

public sealed class CbrExchangeRateProviderTests
{
    [Fact]
    public async Task GetRatesAsync_RequestsMoscowDateAndReadsAFreshXmlResponseEachTime()
    {
        // Arrange
        var requests = new List<string>();
        var encoding = CodePagesEncodingProvider.Instance.GetEncoding(1251)!;
        using var client = new HttpClient(new StubMessageHandler(request =>
        {
            requests.Add(request.RequestUri!.PathAndQuery);
            var dollar = requests.Count == 1 ? "82,1911" : "83,1911";
            var xml = $"""
                <?xml version="1.0" encoding="windows-1251"?>
                <ValCurs Date="05.07.2026">
                    <Valute><CharCode>USD</CharCode><Nominal>1</Nominal><Name>Доллар США</Name><Value>{dollar}</Value></Valute>
                    <Valute><CharCode>EUR</CharCode><Nominal>10</Nominal><Name>Евро</Name><Value>903,4450</Value></Valute>
                    <Valute><CharCode>GBP</CharCode><Nominal>0</Nominal><Value>105,7000</Value></Valute>
                    <Valute><CharCode>JPY</CharCode><Nominal>100</Nominal><Value>Invalid</Value></Valute>
                </ValCurs>
                """;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(encoding.GetBytes(xml)) };
        })) { BaseAddress = new Uri("https://www.cbr.ru/") };
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 4, 22, 30, 0, TimeSpan.Zero));
        var provider = new CbrExchangeRateProvider(client, clock, NullLogger<CbrExchangeRateProvider>.Instance);

        // Act
        var first = await provider.GetRatesAsync(CancellationToken.None);
        var second = await provider.GetRatesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(new DateOnly(2026, 7, 5), first.RateDate);
        Assert.Equal(1m, first.RublesPerUnit["RUB"]);
        Assert.Equal(82.1911m, first.RublesPerUnit["USD"]);
        Assert.Equal(90.3445m, first.RublesPerUnit["EUR"]);
        Assert.DoesNotContain("GBP", first.RublesPerUnit.Keys);
        Assert.DoesNotContain("JPY", first.RublesPerUnit.Keys);
        Assert.Equal(83.1911m, second.RublesPerUnit["USD"]);
        Assert.Equal(["/scripts/XML_daily.asp?date_req=05/07/2026", "/scripts/XML_daily.asp?date_req=05/07/2026"], requests);
    }

    [Fact]
    public async Task GetRatesAsync_WhenUpstreamFails_ReturnsNoRates()
    {
        // Arrange
        using var client = new HttpClient(new StubMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
        {
            BaseAddress = new Uri("https://www.cbr.ru/")
        };
        var provider = new CbrExchangeRateProvider(client, TimeProvider.System, NullLogger<CbrExchangeRateProvider>.Instance);

        // Act
        var rates = await provider.GetRatesAsync(CancellationToken.None);

        // Assert
        Assert.Null(rates);
    }

    private sealed class StubMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(getResponse(request));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
