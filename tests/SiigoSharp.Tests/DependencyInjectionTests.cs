using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AddSiigo_registers_a_working_singleton()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers/c-1", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        var services = new ServiceCollection();
        services.AddSiigo(Configure).ConfigurePrimaryHttpMessageHandler(() => siigo);
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        var client = provider.GetRequiredService<ISiigoClient>();
        var customer = await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        Assert.Equal("13832081", customer.Identification);
        Assert.Same(client, provider.GetRequiredService<ISiigoClient>());
        Assert.Equal(1, siigo.TokensIssued);
        Assert.All(siigo.Requests, request => Assert.Equal("SiigoSharpTests", request.PartnerId));
    }

    [Fact]
    public async Task Keeps_ID_numbers_and_tokens_out_of_the_HttpClient_logs()
    {
        var logs = new TestLoggerProvider();
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers", _ => FakeSiigoHandler.Json(HttpStatusCode.OK,
            """{"pagination":{"page":1,"page_size":25,"total_results":0},"results":[]}"""));
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));
        services.AddSiigo(Configure).ConfigurePrimaryHttpMessageHandler(() => siigo);
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ISiigoClient>().Customers
            .ListAsync(new CustomerQuery { Identification = "1032456789" }, TestContext.Current.CancellationToken);

        Assert.Contains(logs.Entries, entry => entry.Category.StartsWith("SiigoSharp", StringComparison.Ordinal));
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain("1032456789", entry.Message);
            Assert.DoesNotContain("token-1", entry.Message);
        });
    }

    [Fact]
    public void AddSiigo_binds_a_configuration_section()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Siigo:Username"] = "api@empresa.com",
                ["Siigo:AccessKey"] = "secret-access-key",
                ["Siigo:PartnerId"] = "MiIntegracion",
                ["Siigo:Language"] = "English",
                ["Siigo:Retry:MaxRetries"] = "6",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSiigo(configuration.GetSection("Siigo"));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<SiigoClientOptions>>().Value;

        Assert.Equal("MiIntegracion", options.PartnerId);
        Assert.Equal(SiigoLanguage.English, options.Language);
        Assert.Equal(6, options.Retry.MaxRetries);
    }

    [Fact]
    public void AddSiigo_validates_the_options()
    {
        var services = new ServiceCollection();
        services.AddSiigo(_ => { });
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<ISiigoClient>());

        Assert.Equal(3, exception.Failures.Count());
    }

    [Fact]
    public async Task Calling_AddSiigo_twice_adds_configuration_without_duplicating_handlers()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers/c-1", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));
        var services = new ServiceCollection();
        services.AddSiigo(options =>
        {
            Configure(options);
            options.Retry.MaxRetries = 1;
            options.Retry.BaseDelay = TimeSpan.Zero;
        }).ConfigurePrimaryHttpMessageHandler(() => siigo);
        services.AddSiigo(options => options.Language = SiigoLanguage.English);
        await using var provider = services.BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<SiigoApiException>(() =>
            provider.GetRequiredService<ISiigoClient>().Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        // One retry handler: 1 attempt + 1 retry. Two handlers would make 4 requests.
        Assert.Equal(2, siigo.RequestsTo("/v1/customers/c-1").Count);
        Assert.StartsWith("Siigo returned HTTP 503", exception.Message);
    }

    private static void Configure(SiigoClientOptions options)
    {
        options.Username = "api@empresa.com";
        options.AccessKey = "secret-access-key";
        options.PartnerId = "SiigoSharpTests";
    }
}
