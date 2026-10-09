using System.Net;
using System.Text.Json;
using SiigoSharp.Internal;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class ErrorHandlingTests
{
    [Fact]
    public async Task Turns_Siigo_errors_into_SiigoApiException()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/customers", _ =>
            FakeSiigoHandler.SiigoError(HttpStatusCode.BadRequest, "already_exists", "The identification already exists", "identification"));
        using var client = TestClient.Create(siigo);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(
            () => client.Customers.CreateAsync(Fixtures.CustomerRequest(), TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("already_exists", error.Code);
        Assert.Equal("The identification already exists", error.Message);
        Assert.Equal(["identification"], error.Params);
        Assert.True(exception.HasErrorCode("ALREADY_EXISTS"));
        Assert.Contains("already_exists", exception.ResponseBody);
    }

    [Theory]
    [InlineData(SiigoLanguage.Spanish,
        "Siigo respondió HTTP 400 BadRequest a POST /v1/customers. [already_exists] The identification already exists (identification)")]
    [InlineData(SiigoLanguage.English,
        "Siigo returned HTTP 400 BadRequest for POST /v1/customers. [already_exists] The identification already exists (identification)")]
    public async Task Writes_the_message_in_the_configured_language(SiigoLanguage language, string expected)
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/customers", _ =>
            FakeSiigoHandler.SiigoError(HttpStatusCode.BadRequest, "already_exists", "The identification already exists", "identification"));
        using var client = TestClient.Create(siigo, options => options.Language = language);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(
            () => client.Customers.CreateAsync(Fixtures.CustomerRequest(), TestContext.Current.CancellationToken));

        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task Keeps_query_strings_out_of_error_messages()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.BadRequest, "{}"));
        using var client = TestClient.Create(siigo);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(() =>
            client.Customers.ListAsync(new CustomerQuery { Identification = "1032456789" }, TestContext.Current.CancellationToken));

        Assert.Contains("GET /v1/customers.", exception.Message);
        Assert.DoesNotContain("1032456789", exception.Message);
    }

    [Fact]
    public async Task Reports_a_response_that_is_not_JSON_as_SiigoException()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers/c-1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>Mantenimiento</html>") });
        using var client = TestClient.Create(siigo, options => options.Language = SiigoLanguage.English);

        var exception = await Assert.ThrowsAsync<SiigoException>(
            () => client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        Assert.StartsWith("Could not read the response from Siigo for GET /v1/customers/c-1", exception.Message);
        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData("""{"Status":400,"Errors":[{"Code":"invalid","Message":"PascalCase","Params":["code"],"Detail":"See docs"}]}""", "invalid", "PascalCase")]
    [InlineData("""{"status":400,"errors":[{"code":"invalid","message":"lower case"}]}""", "invalid", "lower case")]
    [InlineData("""{"Code":"single","Message":"A single object"}""", "single", "A single object")]
    [InlineData("""{"error":"invalid_grant","error_description":"OAuth style"}""", "invalid_grant", "OAuth style")]
    [InlineData("""[{"Code":"array","Message":"A top-level array"}]""", "array", "A top-level array")]
    public void Reads_the_error_shapes_found_in_Siigo_documentation(string body, string code, string message)
    {
        var error = Assert.Single(SiigoErrorParser.Parse(body));

        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("""{"unexpected":true}""")]
    [InlineData("""{"Errors":"not a list"}""")]
    public void Returns_no_errors_for_unknown_bodies(string? body) =>
        Assert.Empty(SiigoErrorParser.Parse(body));
}
