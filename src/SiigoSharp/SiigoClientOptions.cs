namespace SiigoSharp;

/// <summary>
/// Settings for <see cref="SiigoClient"/>. Set them in code or bind them from configuration
/// (for example the <c>"Siigo"</c> section of <c>appsettings.json</c>).
/// </summary>
public sealed class SiigoClientOptions
{
    /// <summary>The default address of the Siigo API: <c>https://api.siigo.com/</c>.</summary>
    public static Uri DefaultBaseAddress { get; } = new("https://api.siigo.com/");

    /// <summary>The Siigo API user, usually the e-mail address of the user enabled for the API.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>The access key generated in Siigo Nube for API integrations. Keep it out of source control.</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// The name of your application: 3 to 100 letters or digits, without spaces. It is sent in the
    /// <c>Partner-Id</c> header, which Siigo requires on every request, including authentication.
    /// </summary>
    public string PartnerId { get; set; } = string.Empty;

    /// <summary>The address of the Siigo API. Defaults to <see cref="DefaultBaseAddress"/>.</summary>
    public Uri BaseAddress { get; set; } = DefaultBaseAddress;

    /// <summary>
    /// The language of exception and log messages. Defaults to <see cref="SiigoLanguage.Spanish"/>.
    /// </summary>
    public SiigoLanguage Language { get; set; } = SiigoLanguage.Spanish;

    /// <summary>
    /// The maximum duration of a call, including its retries. Defaults to 120 seconds, the minimum Siigo
    /// recommends because creating documents can be slow. Use
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to rely only on cancellation tokens.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// How long before its expiration the access token is renewed. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan TokenRefreshMargin { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>The retry policy for transient errors.</summary>
    public SiigoRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// A copy taken when a client is created, so that later changes to this object (for example, reusing it
    /// with another company's credentials) never reach that client.
    /// </summary>
    internal SiigoClientOptions Clone() => new()
    {
        Username = Username,
        AccessKey = AccessKey,
        PartnerId = PartnerId,
        BaseAddress = BaseAddress,
        Language = Language,
        Timeout = Timeout,
        TokenRefreshMargin = TokenRefreshMargin,
        Retry = new SiigoRetryOptions
        {
            MaxRetries = Retry.MaxRetries,
            BaseDelay = Retry.BaseDelay,
            MaxDelay = Retry.MaxDelay,
        },
    };
}
