using System.Net;
using System.Net.Http.Headers;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class AuthenticationTests
{
    private const string CustomerPath = "/v1/customers/c-1";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Requests_a_token_once_and_reuses_it()
    {
        var siigo = FakeWithCustomer();
        using var client = TestClient.Create(siigo);

        await client.Customers.GetAsync("c-1", Cancellation);
        await client.Customers.GetAsync("c-1", Cancellation);

        Assert.Equal(1, siigo.TokensIssued);
        Assert.All(siigo.RequestsTo(CustomerPath), request => Assert.Equal("Bearer token-1", request.Authorization));
    }

    [Fact]
    public async Task Sends_the_credentials_and_the_partner_id()
    {
        var siigo = FakeWithCustomer();
        using var client = TestClient.Create(siigo);

        await client.Customers.GetAsync("c-1", Cancellation);

        var auth = Assert.Single(siigo.RequestsTo("/auth"));
        Assert.Equal(HttpMethod.Post, auth.Method);
        Assert.Equal("api@empresa.com", (string?)auth.BodyJson["username"]);
        Assert.Equal("secret-access-key", (string?)auth.BodyJson["access_key"]);
        Assert.Null(auth.Authorization);
        Assert.All(siigo.Requests, request => Assert.Equal("SiigoSharpTests", request.PartnerId));
    }

    [Fact]
    public async Task Renews_the_token_in_the_background_shortly_before_it_expires()
    {
        var time = new TestTimeProvider();
        var siigo = FakeWithCustomer();
        using var client = TestClient.Create(siigo, timeProvider: time);
        await client.Customers.GetAsync("c-1", Cancellation);

        // Six minutes before the 24 hours: outside the default 5-minute margin, nothing is renewed.
        time.Advance(TimeSpan.FromHours(24) - TimeSpan.FromMinutes(6));
        await client.Customers.GetAsync("c-1", Cancellation);
        Assert.Equal(1, siigo.TokensIssued);

        // Four minutes before: the call goes on with the valid token while a new one is requested.
        time.Advance(TimeSpan.FromMinutes(2));
        await client.Customers.GetAsync("c-1", Cancellation);
        Assert.Equal("Bearer token-1", siigo.RequestsTo(CustomerPath)[^1].Authorization);

        await Eventually(async () =>
        {
            await client.Customers.GetAsync("c-1", Cancellation);
            return siigo.RequestsTo(CustomerPath)[^1].Authorization == "Bearer token-2";
        });
        Assert.Equal(2, siigo.TokensIssued);
    }

    [Fact]
    public async Task Keeps_using_a_valid_token_when_a_renewal_in_the_background_fails()
    {
        var time = new TestTimeProvider();
        var logs = new TestLoggerProvider();
        var authAvailable = true;
        var siigo = FakeWithCustomer().On(HttpMethod.Post, "/auth", _ => Volatile.Read(ref authAvailable)
            ? FakeSiigoHandler.Json(HttpStatusCode.Created, new { access_token = "token-1", expires_in = 86400 })
            : FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));
        using var loggerFactory = logs.CreateFactory();
        using var client = TestClient.Create(siigo, timeProvider: time, loggerFactory: loggerFactory);
        await client.Customers.GetAsync("c-1", Cancellation);

        Volatile.Write(ref authAvailable, false);
        time.Advance(TimeSpan.FromHours(24) - TimeSpan.FromMinutes(4));
        await client.Customers.GetAsync("c-1", Cancellation);
        await Eventually(() => Task.FromResult(logs.Entries.Any(entry => entry.EventId.Name == "TokenRenewalFailed")));

        // One background renewal with its own retries (1 + MaxRetries) failed...
        Assert.Equal(1 + 5, siigo.RequestsTo("/auth").Count);

        // ...yet calls keep working with the valid token, without hitting the failing endpoint again right away.
        await client.Customers.GetAsync("c-1", Cancellation);
        Assert.Equal("Bearer token-1", siigo.RequestsTo(CustomerPath)[^1].Authorization);
        Assert.Equal(1 + 5, siigo.RequestsTo("/auth").Count);
    }

    [Fact]
    public async Task Renews_a_rejected_token_and_repeats_the_request()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, CustomerPath, request =>
            request.Authorization == "Bearer token-1"
                ? FakeSiigoHandler.SiigoError(HttpStatusCode.Unauthorized, "unauthorized", "Invalid token")
                : FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        using var client = TestClient.Create(siigo);

        var customer = await client.Customers.GetAsync("c-1", Cancellation);

        Assert.Equal("13832081", customer.Identification);
        Assert.Equal(2, siigo.TokensIssued);
        Assert.Equal(["Bearer token-1", "Bearer token-2"], siigo.RequestsTo(CustomerPath).Select(request => request.Authorization));
    }

    [Fact]
    public async Task Concurrent_calls_share_a_single_token_request()
    {
        var siigo = FakeWithCustomer().OnAsync(HttpMethod.Post, "/auth", async _ =>
        {
            // A slow auth endpoint makes every call wait for the token at once.
            await Task.Delay(50, Cancellation);
            return FakeSiigoHandler.Json(HttpStatusCode.Created, new { access_token = "shared", expires_in = 86400 });
        });
        using var client = TestClient.Create(siigo);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => client.Customers.GetAsync("c-1", Cancellation)));

        Assert.Single(siigo.RequestsTo("/auth"));
    }

    [Fact]
    public async Task Concurrent_rejections_renew_the_token_once()
    {
        var revoked = false;
        var siigo = new FakeSiigoHandler().OnAsync(HttpMethod.Get, CustomerPath, async request =>
        {
            await Task.Delay(20, Cancellation);
            return Volatile.Read(ref revoked) && request.Authorization == "Bearer token-1"
                ? FakeSiigoHandler.SiigoError(HttpStatusCode.Unauthorized, "unauthorized", "Invalid token")
                : FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson);
        });
        using var client = TestClient.Create(siigo);
        await client.Customers.GetAsync("c-1", Cancellation);

        Volatile.Write(ref revoked, true);
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.Customers.GetAsync("c-1", Cancellation)));

        Assert.Equal(2, siigo.TokensIssued);
    }

    [Fact]
    public async Task Concurrent_calls_share_a_failed_token_request()
    {
        var siigo = FakeWithCustomer().OnAsync(HttpMethod.Post, "/auth", async _ =>
        {
            await Task.Delay(50, Cancellation);
            return FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}");
        });
        using var client = TestClient.Create(siigo);

        var calls = Enumerable.Range(0, 10).Select(_ => client.Customers.GetAsync("c-1", Cancellation)).ToList();
        foreach (var call in calls)
        {
            var exception = await Assert.ThrowsAsync<SiigoApiException>(() => call);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        }

        // One token request with its own retries (1 + MaxRetries), shared by the ten calls.
        Assert.Equal(1 + 4, siigo.RequestsTo("/auth").Count);
    }

    [Fact]
    public async Task A_caller_that_gives_up_does_not_cancel_the_token_request_of_the_others()
    {
        var siigo = FakeWithCustomer().OnAsync(HttpMethod.Post, "/auth", async _ =>
        {
            await Task.Delay(200, Cancellation);
            return FakeSiigoHandler.Json(HttpStatusCode.Created, new { access_token = "shared", expires_in = 86400 });
        });
        using var client = TestClient.Create(siigo);
        using var impatient = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        impatient.CancelAfter(TimeSpan.FromMilliseconds(50));

        var gaveUp = client.Customers.GetAsync("c-1", impatient.Token);
        var waited = client.Customers.GetAsync("c-1", Cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gaveUp);
        Assert.Equal("13832081", (await waited).Identification);
        Assert.Single(siigo.RequestsTo("/auth"));
    }

    [Theory]
    [InlineData(SiigoLanguage.Spanish, "Siigo rechazó la autenticación (HTTP 401 Unauthorized). Verifique Username, AccessKey y PartnerId.")]
    [InlineData(SiigoLanguage.English, "Siigo rejected the authentication (HTTP 401 Unauthorized). Check Username, AccessKey and PartnerId.")]
    public async Task Rejected_credentials_throw_SiigoAuthenticationException(SiigoLanguage language, string expectedMessage)
    {
        var siigo = FakeWithCustomer().On(HttpMethod.Post, "/auth", _ =>
            FakeSiigoHandler.SiigoError(HttpStatusCode.Unauthorized, "unauthorized", "Invalid username or access key"));
        using var client = TestClient.Create(siigo, options => options.Language = language);

        var exception = await Assert.ThrowsAsync<SiigoAuthenticationException>(() => client.Customers.GetAsync("c-1", Cancellation));

        Assert.StartsWith(expectedMessage, exception.Message);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.True(exception.HasErrorCode("unauthorized"));
        Assert.Empty(siigo.RequestsTo(CustomerPath));
    }

    [Fact]
    public async Task Reports_an_unavailable_auth_endpoint_as_a_service_error_rather_than_bad_credentials()
    {
        var siigo = FakeWithCustomer().On(HttpMethod.Post, "/auth", _ =>
        {
            var response = FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}");
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
            return response;
        });
        using var client = TestClient.Create(siigo, options => options.Language = SiigoLanguage.English);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(() => client.Customers.GetAsync("c-1", Cancellation));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(5), exception.RetryAfter);
        Assert.StartsWith("Siigo returned HTTP 503 ServiceUnavailable for POST /auth.", exception.Message);
    }

    [Fact]
    public async Task Does_not_multiply_retries_when_the_auth_endpoint_is_unreachable()
    {
        var siigo = FakeWithCustomer().On(HttpMethod.Post, "/auth", _ =>
            throw new HttpRequestException(HttpRequestError.NameResolutionError, "No such host is known."));
        using var client = TestClient.Create(siigo);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.Customers.GetAsync("c-1", Cancellation));

        // The token request is retried on its own; the call waiting for it does not repeat those retries.
        Assert.Equal(1 + 4, siigo.RequestsTo("/auth").Count);
        Assert.Empty(siigo.RequestsTo(CustomerPath));
    }

    private static FakeSiigoHandler FakeWithCustomer() =>
        new FakeSiigoHandler().On(HttpMethod.Get, CustomerPath, _ => FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));

    private static async Task Eventually(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 300; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(10, Cancellation);
        }

        Assert.Fail("The condition was not met in time.");
    }
}
