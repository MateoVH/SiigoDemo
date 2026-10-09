using System.Net.Http.Headers;
using System.Reflection;

namespace SiigoSharp.Internal;

/// <summary>Settings shared by the <see cref="HttpClient"/> that talks to Siigo.</summary>
internal static class SiigoHttp
{
    public const string ClientName = "SiigoSharp";

    /// <summary>Marks the token request, which must not carry (or wait for) an access token.</summary>
    public static readonly HttpRequestOptionsKey<bool> SkipAuthentication = new("SiigoSharp.SkipAuthentication");

    /// <summary>Marks a POST that can safely be repeated after a server error, such as requesting a token.</summary>
    public static readonly HttpRequestOptionsKey<bool> Idempotent = new("SiigoSharp.Idempotent");

    /// <summary>
    /// Set in <see cref="Exception.Data"/> when obtaining the token failed after its own retries, so the
    /// request that was waiting for the token does not repeat them all over again.
    /// </summary>
    public const string TokenRequestFailed = "SiigoSharp.TokenRequestFailed";

    private static readonly ProductInfoHeaderValue UserAgent = new("SiigoSharp", GetVersion());

    public static void Configure(HttpClient client, SiigoClientOptions options)
    {
        client.BaseAddress = WithTrailingSlash(options.BaseAddress);
        client.Timeout = options.Timeout;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.Add(UserAgent);
        client.DefaultRequestHeaders.Add("Partner-Id", options.PartnerId);
    }

    public static bool IsMarked(HttpRequestMessage request, HttpRequestOptionsKey<bool> key) =>
        request.Options.TryGetValue(key, out var value) && value;

    // Without the trailing slash, a base address with a path (e.g. a proxy) would lose its last segment.
    private static Uri WithTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");

    private static string GetVersion()
    {
        var version = typeof(SiigoHttp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return version?.Split('+')[0] ?? "0.0.0";
    }
}
