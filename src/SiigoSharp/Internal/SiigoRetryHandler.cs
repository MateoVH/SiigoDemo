using System.Net;
using Microsoft.Extensions.Logging;

namespace SiigoSharp.Internal;

/// <summary>
/// Retries transient failures with exponential backoff and jitter, honoring <c>Retry-After</c>.
/// <see cref="SiigoRetryOptions"/> describes which failures are retried for which HTTP methods.
/// </summary>
internal sealed class SiigoRetryHandler : DelegatingHandler
{
    private readonly SiigoRetryOptions _options;
    private readonly SiigoLanguage _language;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    public SiigoRetryHandler(SiigoClientOptions options, TimeProvider timeProvider, ILogger logger)
    {
        _options = options.Retry;
        _language = options.Language;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var canRetry = attempt <= _options.MaxRetries;

            HttpResponseMessage response;
            try
            {
                response = await SendAndBufferAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception) when (canRetry && IsTransient(request, exception))
            {
                await WaitAsync(request, exception.HttpRequestError.ToString(), Backoff(attempt), attempt, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (!canRetry || !IsTransient(request, response.StatusCode))
            {
                return response;
            }

            var retryAfter = RetryAfterHeader.GetDelay(response, _timeProvider);
            if (retryAfter > _options.MaxDelay)
            {
                Log.RetryAfterTooLong(_logger, _language, request, retryAfter.Value, _options.MaxDelay);
                return response;
            }

            var reason = $"HTTP {(int)response.StatusCode}";
            response.Dispose();
            await WaitAsync(request, reason, retryAfter ?? Backoff(attempt), attempt, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 408 and 429 mean the request was not processed, so any method can be repeated. After a 5xx the
    /// request may have been processed, so only idempotent methods are repeated.
    /// </summary>
    internal static bool IsTransient(HttpRequestMessage request, HttpStatusCode status) => status switch
    {
        HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests => true,
        HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => IsIdempotent(request),
        _ => false,
    };

    /// <summary>
    /// Network failures are repeated for idempotent requests. Other requests are repeated only when the
    /// connection could not be opened, because then the request never left this machine.
    /// </summary>
    internal static bool IsTransient(HttpRequestMessage request, HttpRequestException exception) =>
        !exception.Data.Contains(SiigoHttp.TokenRequestFailed) &&
        (IsIdempotent(request) ||
            exception.HttpRequestError is HttpRequestError.NameResolutionError
                or HttpRequestError.ConnectionError
                or HttpRequestError.SecureConnectionError);

    /// <summary>
    /// Reads the whole body before returning, while still inside the retry loop and the HttpClient timeout:
    /// a body that stalls is bounded by <see cref="SiigoClientOptions.Timeout"/>, and a connection that
    /// breaks mid-body is a network failure retried like any other.
    /// </summary>
    private async Task<HttpResponseMessage> SendAndBufferAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        try
        {
            var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var buffered = new ByteArrayContent(body);
            foreach (var header in response.Content.Headers)
            {
                if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            response.Content.Dispose();
            response.Content = buffered;
            return response;
        }
        catch (Exception exception)
        {
            response.Dispose();
            if (exception is IOException ioException)
            {
                var error = ioException is HttpIOException httpIOException ? httpIOException.HttpRequestError : HttpRequestError.Unknown;
                throw new HttpRequestException(error, ioException.Message, ioException);
            }

            throw;
        }
    }

    private static bool IsIdempotent(HttpRequestMessage request)
    {
        var method = request.Method;
        return method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options ||
            method == HttpMethod.Put || method == HttpMethod.Delete ||
            SiigoHttp.IsMarked(request, SiigoHttp.Idempotent);
    }

    private TimeSpan Backoff(int attempt)
    {
        var exponential = _options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitter = 0.8 + (Random.Shared.NextDouble() * 0.4);
        return TimeSpan.FromMilliseconds(Math.Min(exponential * jitter, _options.MaxDelay.TotalMilliseconds));
    }

    private async Task WaitAsync(
        HttpRequestMessage request, string reason, TimeSpan delay, int attempt, CancellationToken cancellationToken)
    {
        Log.Retrying(_logger, _language, request, reason, delay, attempt, _options.MaxRetries);
        await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
    }
}
