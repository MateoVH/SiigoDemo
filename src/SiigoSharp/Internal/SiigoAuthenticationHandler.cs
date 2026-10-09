using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace SiigoSharp.Internal;

/// <summary>
/// Adds the access token to every request. When Siigo answers 401 (the token expired early or was
/// revoked), renews the token once and repeats the request. Siigo rejects unauthenticated requests
/// before processing them, so repeating is safe even for a POST.
/// </summary>
internal sealed class SiigoAuthenticationHandler : DelegatingHandler
{
    private readonly SiigoTokenProvider _tokens;
    private readonly SiigoLanguage _language;
    private readonly ILogger _logger;

    public SiigoAuthenticationHandler(SiigoTokenProvider tokens, SiigoLanguage language, ILogger logger)
    {
        _tokens = tokens;
        _language = language;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (SiigoHttp.IsMarked(request, SiigoHttp.SkipAuthentication))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var token = await _tokens.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        Log.TokenRejected(_logger, _language, request);
        response.Dispose();

        token = await _tokens.RenewAsync(rejectedToken: token, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
