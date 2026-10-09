namespace SiigoSharp;

/// <summary>
/// Retry policy for transient Siigo errors.
/// </summary>
/// <remarks>
/// <para>
/// Responses 408 and 429 (rate limit) mean Siigo did not process the request, so they are retried for every
/// HTTP method. Server errors (500, 502, 503, 504) and network failures are retried for idempotent
/// requests: GET, PUT, DELETE, and invoice creation, which carries an <c>Idempotency-Key</c>.
/// </para>
/// <para>
/// Any other POST, such as creating a customer, is retried only when it never reached Siigo (DNS,
/// connection or TLS failures): after a server error the record may already exist.
/// </para>
/// <para>
/// Siigo allows 100 requests per minute per company (10 for test companies) and does not document a
/// <c>Retry-After</c> header, so the defaults spread four retries over about 30 seconds.
/// </para>
/// </remarks>
public sealed class SiigoRetryOptions
{
    /// <summary>The number of retries after the first attempt. Zero disables retries. Defaults to 4.</summary>
    public int MaxRetries { get; set; } = 4;

    /// <summary>
    /// The delay before the first retry. It doubles on each retry and varies ±20% (jitter) so that many
    /// clients do not retry at the same time. Defaults to 2 seconds.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The longest wait between two attempts. When the <c>Retry-After</c> header asks for a longer wait,
    /// the error is returned right away (see <see cref="SiigoApiException.RetryAfter"/>) instead of
    /// blocking the caller. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}
