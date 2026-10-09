using System.Net.Http.Json;
using System.Text.Json;

namespace SiigoSharp.Internal;

/// <summary>
/// Sends requests to the Siigo API and turns responses into models or into <see cref="SiigoApiException"/>.
/// Authentication and retries happen in the HTTP pipeline, below this class.
/// </summary>
internal sealed class SiigoConnection
{
    private readonly Func<HttpClient> _httpClient;
    private readonly TimeProvider _timeProvider;

    /// <param name="httpClient">
    /// Returns the client to use for each request. With dependency injection it comes from
    /// <see cref="IHttpClientFactory"/>, which keeps rotating connections even though this class is a singleton.
    /// </param>
    /// <param name="messages">Exception texts in the configured language.</param>
    /// <param name="timeProvider">The clock used to read <c>Retry-After</c> dates.</param>
    public SiigoConnection(Func<HttpClient> httpClient, Messages messages, TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        Messages = messages;
        _timeProvider = timeProvider;
    }

    public Messages Messages { get; }

    public Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken);

    public Task<T> PostAsync<TBody, T>(string uri, TBody body, CancellationToken cancellationToken) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, uri) { Content = SiigoJson.CreateContent(body) }, cancellationToken);

    /// <summary>
    /// Sends a POST with an <c>Idempotency-Key</c>: when Siigo receives the same key twice it returns the
    /// document it already created, so the request can be retried like a GET.
    /// </summary>
    public Task<T> PostIdempotentAsync<TBody, T>(string uri, TBody body, string idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = SiigoJson.CreateContent(body) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Options.Set(SiigoHttp.Idempotent, true);
        return SendAsync<T>(request, cancellationToken);
    }

    public Task<T> PutAsync<TBody, T>(string uri, TBody body, CancellationToken cancellationToken) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Put, uri) { Content = SiigoJson.CreateContent(body) }, cancellationToken);

    public async Task DeleteAsync(string uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        using var response = await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            using var response = await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.Content.Headers.ContentLength == 0)
            {
                throw new SiigoException(Messages.EmptyResponse(request.Method, request.RequestUri));
            }

            T? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<T>(SiigoJson.Options, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException exception)
            {
                throw new SiigoException(Messages.InvalidResponse(request.Method, request.RequestUri), exception);
            }

            return result ?? throw new SiigoException(Messages.EmptyResponse(request.Method, request.RequestUri));
        }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _httpClient()
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            var body = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
            var errors = SiigoErrorParser.Parse(body);
            throw new SiigoApiException(
                Messages.ApiError(request.Method, request.RequestUri, response.StatusCode, errors),
                response.StatusCode,
                errors,
                body,
                RetryAfterHeader.GetDelay(response, _timeProvider));
        }
    }

    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            // The status code is what matters; a body cut off by the network should not hide it.
            return null;
        }
    }
}
