using System.Net;
using SiigoSharp.Internal;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class RetryTests
{
    private const string CustomerPath = "/v1/customers/c-1";

    [Fact]
    public async Task Repeats_a_GET_after_transient_server_errors()
    {
        var siigo = new FakeSiigoHandler().OnSequence(HttpMethod.Get, CustomerPath,
            () => FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"),
            () => FakeSiigoHandler.Json(HttpStatusCode.BadGateway, "{}"),
            () => FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        var time = new TestTimeProvider();
        using var client = TestClient.Create(siigo, timeProvider: time);

        var customer = await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        Assert.Equal("13832081", customer.Identification);
        Assert.Equal(3, siigo.RequestsTo(CustomerPath).Count);
        Assert.Equal(2, time.Delays.Count);
    }

    [Fact]
    public async Task Does_not_repeat_a_POST_without_idempotency_after_a_server_error()
    {
        // The customer may have been created before the error: sending it again could duplicate it.
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/customers", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.InternalServerError, "{}"));
        using var client = TestClient.Create(siigo);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(
            () => client.Customers.CreateAsync(Fixtures.CustomerRequest(), TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Single(siigo.RequestsTo("/v1/customers"));
    }

    [Fact]
    public async Task Repeats_invoice_creation_with_the_same_idempotency_key()
    {
        var siigo = new FakeSiigoHandler().OnSequence(HttpMethod.Post, "/v1/invoices",
            () => FakeSiigoHandler.Json(HttpStatusCode.InternalServerError, "{}"),
            () => FakeSiigoHandler.Json(HttpStatusCode.Created, Fixtures.InvoiceJson));
        using var client = TestClient.Create(siigo);

        var invoice = await client.Invoices.CreateAsync(Fixtures.InvoiceRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("FV-2-22", invoice.Name);
        var attempts = siigo.RequestsTo("/v1/invoices");
        Assert.Equal(2, attempts.Count);
        Assert.NotNull(attempts[0].IdempotencyKey);
        Assert.Equal(attempts[0].IdempotencyKey, attempts[1].IdempotencyKey);
        Assert.Equal(attempts[0].Body, attempts[1].Body);
    }

    [Fact]
    public async Task Repeats_a_POST_rejected_by_the_rate_limit_after_Retry_After()
    {
        var siigo = new FakeSiigoHandler().OnSequence(HttpMethod.Post, "/v1/customers",
            () => RateLimited(TimeSpan.FromSeconds(3)),
            () => FakeSiigoHandler.Json(HttpStatusCode.Created, Fixtures.CustomerJson));
        var time = new TestTimeProvider();
        using var client = TestClient.Create(siigo, timeProvider: time);

        await client.Customers.CreateAsync(Fixtures.CustomerRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(2, siigo.RequestsTo("/v1/customers").Count);
        Assert.Equal([TimeSpan.FromSeconds(3)], time.Delays);
    }

    [Fact]
    public async Task Returns_the_error_when_Retry_After_exceeds_the_maximum_delay()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, CustomerPath, _ => RateLimited(TimeSpan.FromMinutes(2)));
        var time = new TestTimeProvider();
        using var client = TestClient.Create(siigo, timeProvider: time);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(
            () => client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(2), exception.RetryAfter);
        Assert.True(exception.HasErrorCode("requests_limit"));
        Assert.Single(siigo.RequestsTo(CustomerPath));
        Assert.Empty(time.Delays);
    }

    [Fact]
    public async Task Gives_up_after_MaxRetries()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, CustomerPath, _ =>
            FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));
        using var client = TestClient.Create(siigo, options => options.Retry.MaxRetries = 2);

        var exception = await Assert.ThrowsAsync<SiigoApiException>(
            () => client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(3, siigo.RequestsTo(CustomerPath).Count);
    }

    [Fact]
    public async Task Doubles_the_delay_on_each_retry_with_jitter()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, CustomerPath, _ =>
            FakeSiigoHandler.Json(HttpStatusCode.ServiceUnavailable, "{}"));
        var time = new TestTimeProvider();
        using var client = TestClient.Create(siigo, options =>
        {
            options.Retry.MaxRetries = 3;
            options.Retry.BaseDelay = TimeSpan.FromSeconds(1);
        }, time);

        await Assert.ThrowsAsync<SiigoApiException>(() => client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        Assert.Collection(
            time.Delays,
            delay => Assert.InRange(delay.TotalSeconds, 0.8, 1.2),
            delay => Assert.InRange(delay.TotalSeconds, 1.6, 2.4),
            delay => Assert.InRange(delay.TotalSeconds, 3.2, 4.8));
    }

    [Theory]
    [InlineData(HttpRequestError.NameResolutionError, true)]
    [InlineData(HttpRequestError.ConnectionError, true)]
    [InlineData(HttpRequestError.SecureConnectionError, true)]
    [InlineData(HttpRequestError.ResponseEnded, false)]
    [InlineData(HttpRequestError.Unknown, false)]
    public async Task Repeats_a_POST_after_a_network_failure_only_if_it_never_left_the_machine(HttpRequestError error, bool repeated)
    {
        var attempts = 0;
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/customers", _ =>
            Interlocked.Increment(ref attempts) == 1
                ? throw new HttpRequestException(error, "Simulated network failure")
                : FakeSiigoHandler.Json(HttpStatusCode.Created, Fixtures.CustomerJson));
        using var client = TestClient.Create(siigo);

        var create = () => client.Customers.CreateAsync(Fixtures.CustomerRequest(), TestContext.Current.CancellationToken);

        if (repeated)
        {
            await create();
            Assert.Equal(2, attempts);
        }
        else
        {
            await Assert.ThrowsAsync<HttpRequestException>(create);
            Assert.Equal(1, attempts);
        }
    }

    [Fact]
    public async Task Repeats_a_GET_whose_response_body_was_cut_off()
    {
        var siigo = new FakeSiigoHandler().OnSequence(HttpMethod.Get, CustomerPath,
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new BrokenContent() },
            () => FakeSiigoHandler.Json(HttpStatusCode.OK, Fixtures.CustomerJson));
        using var client = TestClient.Create(siigo);

        var customer = await client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken);

        Assert.Equal("13832081", customer.Identification);
        Assert.Equal(2, siigo.RequestsTo(CustomerPath).Count);
    }

    [Fact]
    public async Task Does_not_repeat_a_POST_whose_response_body_was_cut_off()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/customers", _ =>
            new HttpResponseMessage(HttpStatusCode.Created) { Content = new BrokenContent() });
        using var client = TestClient.Create(siigo);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.Customers.CreateAsync(Fixtures.CustomerRequest(), TestContext.Current.CancellationToken));

        Assert.Single(siigo.RequestsTo("/v1/customers"));
    }

    // Without the protection under test this call would hang; the test timeout turns that into a failure.
    [Fact(Timeout = 10_000)]
    public async Task Applies_the_timeout_to_a_response_body_that_never_ends()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, CustomerPath, _ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new EndlessContent() });
        using var client = TestClient.Create(siigo, options => options.Timeout = TimeSpan.FromMilliseconds(300));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"The call took {stopwatch.Elapsed}.");
    }

    [Theory]
    [InlineData("GET", HttpStatusCode.ServiceUnavailable, true)]
    [InlineData("GET", HttpStatusCode.InternalServerError, true)]
    [InlineData("PUT", HttpStatusCode.BadGateway, true)]
    [InlineData("DELETE", HttpStatusCode.GatewayTimeout, true)]
    [InlineData("POST", HttpStatusCode.InternalServerError, false)]
    [InlineData("POST", HttpStatusCode.ServiceUnavailable, false)]
    [InlineData("POST", HttpStatusCode.TooManyRequests, true)]
    [InlineData("POST", HttpStatusCode.RequestTimeout, true)]
    [InlineData("GET", HttpStatusCode.BadRequest, false)]
    [InlineData("GET", HttpStatusCode.Unauthorized, false)]
    [InlineData("GET", HttpStatusCode.NotFound, false)]
    public void Classifies_responses_as_transient_by_status_and_method(string method, HttpStatusCode status, bool transient)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "https://api.siigo.com/v1/customers");

        Assert.Equal(transient, SiigoRetryHandler.IsTransient(request, status));
    }

    private static HttpResponseMessage RateLimited(TimeSpan retryAfter)
    {
        var response = FakeSiigoHandler.SiigoError(HttpStatusCode.TooManyRequests, "requests_limit", "Too many requests");
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter);
        return response;
    }

    /// <summary>A body whose connection breaks halfway through.</summary>
    private sealed class BrokenContent : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            await stream.WriteAsync("""{"id":"c-"""u8.ToArray());
            throw new IOException("The response ended prematurely.");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    /// <summary>A body that starts and then stalls forever.</summary>
    private sealed class EndlessContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(
            Stream stream, System.Net.TransportContext? context, CancellationToken cancellationToken)
        {
            await stream.WriteAsync("""{"id":"""u8.ToArray(), cancellationToken);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
