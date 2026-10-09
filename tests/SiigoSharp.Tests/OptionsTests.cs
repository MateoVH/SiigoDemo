using System.Net;
using Microsoft.Extensions.Options;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class OptionsTests
{
    [Theory]
    [InlineData(SiigoLanguage.Spanish, "La opción 'Username' es obligatoria.", "La opción 'PartnerId' es obligatoria.")]
    [InlineData(SiigoLanguage.English, "The 'Username' option is required.", "The 'PartnerId' option is required.")]
    public void Rejects_missing_credentials_in_the_configured_language(SiigoLanguage language, string username, string partnerId)
    {
        var exception = Assert.Throws<OptionsValidationException>(() => new SiigoClient(new SiigoClientOptions { Language = language }));

        Assert.Equal(3, exception.Failures.Count());
        Assert.Contains(username, exception.Failures);
        Assert.Contains(partnerId, exception.Failures);
    }

    [Theory]
    [InlineData("Mi App")]
    [InlineData("mi-app")]
    [InlineData("ab")]
    public void Rejects_a_partner_id_that_Siigo_would_refuse(string partnerId)
    {
        var options = TestClient.CreateOptions(options => options.PartnerId = partnerId);

        var exception = Assert.Throws<OptionsValidationException>(() => new SiigoClient(options));

        Assert.Contains(exception.Failures, failure => failure.Contains("PartnerId", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Uses_the_options_as_they_were_when_the_client_was_created()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers/c-1", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        var options = TestClient.CreateOptions(options =>
        {
            options.Username = "empresa-a@siigo.example";
            options.AccessKey = "key-a";
        });
        using var client = new SiigoClient(options, siigo, loggerFactory: null, new TestTimeProvider());

        // Reusing the same object for another company must not change the client already created.
        options.Username = "empresa-b@siigo.example";
        options.AccessKey = "key-b";
        options.Retry.BaseDelay = TimeSpan.FromSeconds(-1);
        await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        var auth = Assert.Single(siigo.RequestsTo("/auth"));
        Assert.Equal("empresa-a@siigo.example", (string?)auth.BodyJson["username"]);
        Assert.Equal("key-a", (string?)auth.BodyJson["access_key"]);
    }

    [Fact]
    public async Task Keeps_the_path_of_a_custom_base_address()
    {
        var siigo = new FakeSiigoHandler()
            .On(HttpMethod.Post, "/gateway/siigo/auth", _ =>
                FakeSiigoHandler.Json(HttpStatusCode.Created, new { access_token = "t", expires_in = 86400 }))
            .On(HttpMethod.Get, "/gateway/siigo/v1/customers/c-1", _ =>
                FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        using var client = TestClient.Create(siigo, options => options.BaseAddress = new Uri("https://proxy.local/gateway/siigo"));

        await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        Assert.Equal("https://proxy.local/gateway/siigo/v1/customers/c-1", siigo.Requests[^1].Uri.ToString());
    }
}
