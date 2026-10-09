using System.Net;
using Microsoft.Extensions.Logging;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class LoggingTests
{
    [Theory]
    [InlineData(SiigoLanguage.Spanish, "GET /v1/customers/c-1 falló (HTTP 503); reintento 1 de 4 en 0 ms")]
    [InlineData(SiigoLanguage.English, "GET /v1/customers/c-1 failed (HTTP 503); retry 1 of 4 in 0 ms")]
    public async Task Logs_retries_in_the_configured_language(SiigoLanguage language, string expected)
    {
        var logs = new TestLoggerProvider();
        var siigo = new FakeSiigoHandler().OnSequence(HttpMethod.Get, "/v1/customers/c-1",
            () => FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"),
            () => FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        using var loggerFactory = logs.CreateFactory();
        using var client = TestClient.Create(siigo, options =>
        {
            options.Language = language;
            options.Retry.BaseDelay = TimeSpan.Zero;
        }, loggerFactory: loggerFactory);

        await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        var entry = Assert.Single(logs.Entries, entry => entry.EventId.Name == "Retrying");
        Assert.Equal(expected, entry.Message);
        Assert.Equal("SiigoSharp.Http", entry.Category);
        Assert.Equal(LogLevel.Warning, entry.Level);
    }

    [Theory]
    [InlineData(SiigoLanguage.Spanish, "Token de acceso de Siigo obtenido; vence el 2026-01-16 12:00:00Z")]
    [InlineData(SiigoLanguage.English, "Siigo access token acquired; it expires on 2026-01-16 12:00:00Z")]
    public async Task Logs_token_renewals_in_the_configured_language(SiigoLanguage language, string expected)
    {
        var logs = new TestLoggerProvider();
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers/c-1", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        using var loggerFactory = logs.CreateFactory();
        using var client = TestClient.Create(siigo, options => options.Language = language, loggerFactory: loggerFactory);

        await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        var entry = Assert.Single(logs.Entries, entry => entry.EventId.Name == "TokenAcquired");
        Assert.Equal(expected, entry.Message);
        Assert.Equal("SiigoSharp.Authentication", entry.Category);
    }

    [Fact]
    public async Task Never_logs_credentials_tokens_or_query_strings()
    {
        var logs = new TestLoggerProvider();
        var siigo = new FakeSiigoHandler().OnSequence(HttpMethod.Get, "/v1/customers",
            () => FakeSiigoHandler.SiigoError(HttpStatusCode.Unauthorized, "unauthorized", "Expired"),
            () => FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"),
            () => FakeSiigoHandler.Json(HttpStatusCode.OK, """{"pagination":{"page":1,"page_size":25,"total_results":0},"results":[]}"""));
        using var loggerFactory = logs.CreateFactory();
        using var client = TestClient.Create(siigo, options => options.Retry.BaseDelay = TimeSpan.Zero, loggerFactory: loggerFactory);

        await client.Customers.ListAsync(new CustomerQuery { Identification = "1032456789" }, TestContext.Current.CancellationToken);

        Assert.NotEmpty(logs.Entries);
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain("secret-access-key", entry.Message);
            Assert.DoesNotContain("token-", entry.Message);
            Assert.DoesNotContain("1032456789", entry.Message);
        });
    }
}
