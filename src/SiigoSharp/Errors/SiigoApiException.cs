using System.Net;

namespace SiigoSharp;

/// <summary>
/// Siigo answered a request with an error status code (4xx or 5xx), after any retries.
/// </summary>
/// <example>
/// <code>
/// try
/// {
///     await siigo.Customers.CreateAsync(request);
/// }
/// catch (SiigoApiException ex) when (ex.HasErrorCode("already_exists"))
/// {
///     // The customer is already registered in Siigo.
/// }
/// </code>
/// </example>
public class SiigoApiException : SiigoException
{
    /// <summary>Creates the exception.</summary>
    public SiigoApiException(
        string message,
        HttpStatusCode statusCode,
        IReadOnlyList<SiigoError>? errors = null,
        string? responseBody = null,
        TimeSpan? retryAfter = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? [];
        ResponseBody = responseBody;
        RetryAfter = retryAfter;
    }

    /// <summary>The HTTP status code returned by Siigo.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The errors listed by Siigo in the response body. Empty when the body had none.</summary>
    public IReadOnlyList<SiigoError> Errors { get; }

    /// <summary>The raw response body, for diagnostics.</summary>
    public string? ResponseBody { get; }

    /// <summary>How long Siigo asked to wait before trying again (<c>Retry-After</c> header), if it said so.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Whether Siigo reported an error with the given code, such as <c>"already_exists"</c>. Case-insensitive.
    /// </summary>
    public bool HasErrorCode(string code) =>
        Errors.Any(error => string.Equals(error.Code, code, StringComparison.OrdinalIgnoreCase));
}
