using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SiigoSharp.Internal;

/// <summary>
/// Requests and caches the Siigo access token, shared by every request.
/// </summary>
/// <remarks>
/// <para>
/// Inside the renewal margin the current token is still valid: callers keep using it while a new one is
/// requested in the background, so a slow or failing renewal never delays or fails a call. Once the token
/// has expired, or when Siigo rejects it, callers wait for the renewal.
/// </para>
/// <para>
/// Only one token request runs at a time and every waiter shares its outcome, success or failure (Siigo
/// limits calls to its authentication endpoint). The request does not use any caller's cancellation token:
/// a caller that gives up stops waiting, but the others still get the token.
/// </para>
/// </remarks>
internal sealed class SiigoTokenProvider
{
    // Siigo issues 24-hour tokens; this value is used only if a response omits expires_in.
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromHours(24);

    // After a background renewal fails, the next one waits this long, so that an outage of the
    // authentication endpoint is not hit by every call made in the meantime.
    private static readonly TimeSpan BackgroundRenewalCooldown = TimeSpan.FromSeconds(30);

    private readonly Func<HttpClient> _httpClient;
    private readonly SiigoClientOptions _options;
    private readonly Messages _messages;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private AccessToken? _token;
    private Task<AccessToken>? _renewal;
    private DateTimeOffset _nextBackgroundRenewal = DateTimeOffset.MinValue;

    /// <param name="httpClient">Returns the Siigo client; the token request is marked so it skips authentication.</param>
    /// <param name="options">The credentials and the token refresh margin.</param>
    /// <param name="timeProvider">The clock that decides when the token expires.</param>
    /// <param name="logger">Receives token requests and renewal failures.</param>
    public SiigoTokenProvider(Func<HttpClient> httpClient, SiigoClientOptions options, TimeProvider timeProvider, ILogger logger)
    {
        _httpClient = httpClient;
        _options = options;
        _messages = new Messages(options.Language);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Returns a valid token, waiting for a new one only when there is none or it has expired.</summary>
    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var token = Volatile.Read(ref _token);
        var now = _timeProvider.GetUtcNow();
        if (token is not null && now < token.RenewAt)
        {
            return token.Value;
        }

        if (token is not null && now < token.ExpiresAt)
        {
            RenewInBackground(now);
            return token.Value;
        }

        var renewed = await JoinRenewal(rejectedToken: null).WaitAsync(cancellationToken).ConfigureAwait(false);
        return renewed.Value;
    }

    /// <summary>
    /// Replaces a token Siigo rejected. When another caller already replaced it, returns that token
    /// instead of requesting a new one.
    /// </summary>
    public async Task<string> RenewAsync(string rejectedToken, CancellationToken cancellationToken)
    {
        var renewed = await JoinRenewal(rejectedToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        return renewed.Value;
    }

    private void RenewInBackground(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_renewal is { IsCompleted: false } || now < _nextBackgroundRenewal)
            {
                return;
            }

            StartRenewal();
        }
    }

    private Task<AccessToken> JoinRenewal(string? rejectedToken)
    {
        lock (_gate)
        {
            if (_renewal is { IsCompleted: false } running)
            {
                return running;
            }

            // Another caller already replaced the expired or rejected token.
            if (_token is { } current && current.Value != rejectedToken && _timeProvider.GetUtcNow() < current.ExpiresAt)
            {
                return Task.FromResult(current);
            }

            return StartRenewal();
        }
    }

    // Called while holding _gate. Task.Run keeps the request itself outside the lock.
    private Task<AccessToken> StartRenewal()
    {
        var renewal = Task.Run(RequestTokenAsync);
        _renewal = renewal;
        _ = renewal.ContinueWith(
            static (task, state) => ((SiigoTokenProvider)state!).OnRenewalFailed(task.Exception!.InnerException!),
            this,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return renewal;
    }

    private void OnRenewalFailed(Exception exception)
    {
        lock (_gate)
        {
            _nextBackgroundRenewal = _timeProvider.GetUtcNow() + BackgroundRenewalCooldown;
        }

        Log.TokenRenewalFailed(_logger, _messages.Language, exception);
    }

    private async Task<AccessToken> RequestTokenAsync()
    {
        Log.RequestingToken(_logger, _messages.Language);

        using var request = new HttpRequestMessage(HttpMethod.Post, "auth")
        {
            Content = SiigoJson.CreateContent(new TokenRequest(_options.Username, _options.AccessKey)),
        };
        request.Options.Set(SiigoHttp.SkipAuthentication, true);
        request.Options.Set(SiigoHttp.Idempotent, true);

        HttpResponseMessage response;
        try
        {
            // Bounded by the HttpClient timeout; deliberately not by any caller's cancellation token.
            response = await _httpClient().SendAsync(request).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            exception.Data[SiigoHttp.TokenRequestFailed] = true;
            throw;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateErrorAsync(request, response).ConfigureAwait(false);
            }

            TokenResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<TokenResponse>(SiigoJson.Options).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                payload = null;
            }

            if (string.IsNullOrWhiteSpace(payload?.AccessToken))
            {
                throw new SiigoAuthenticationException(_messages.TokenMissing, response.StatusCode);
            }

            var now = _timeProvider.GetUtcNow();
            var lifetime = payload.ExpiresIn > 0 ? TimeSpan.FromSeconds(payload.ExpiresIn) : FallbackLifetime;

            // Renew ahead of time, but never so early that a short-lived token would be renewed on every call.
            var margin = _options.TokenRefreshMargin < lifetime / 2 ? _options.TokenRefreshMargin : lifetime / 2;
            var token = new AccessToken(payload.AccessToken, ExpiresAt: now + lifetime, RenewAt: now + lifetime - margin);

            Volatile.Write(ref _token, token);
            Log.TokenAcquired(_logger, _messages.Language, token.ExpiresAt);
            return token;
        }
    }

    /// <summary>
    /// 400, 401 and 403 mean Siigo refused the credentials or the Partner-Id. Anything else, such as 429 or a
    /// 5xx, is a failure of the service and is reported as such, with its <c>Retry-After</c>.
    /// </summary>
    private async Task<SiigoApiException> CreateErrorAsync(HttpRequestMessage request, HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var errors = SiigoErrorParser.Parse(body);

        return response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            ? new SiigoAuthenticationException(
                _messages.AuthenticationFailed(response.StatusCode, errors), response.StatusCode, errors, body)
            : new SiigoApiException(
                _messages.ApiError(request.Method, request.RequestUri, response.StatusCode, errors),
                response.StatusCode,
                errors,
                body,
                RetryAfterHeader.GetDelay(response, _timeProvider));
    }

    private sealed record AccessToken(string Value, DateTimeOffset ExpiresAt, DateTimeOffset RenewAt);

    private sealed record TokenRequest(string Username, string AccessKey);

    private sealed class TokenResponse
    {
        public string? AccessToken { get; init; }

        public long ExpiresIn { get; init; }
    }
}
