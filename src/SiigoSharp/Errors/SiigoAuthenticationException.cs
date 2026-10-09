using System.Net;

namespace SiigoSharp;

/// <summary>
/// Siigo did not issue an access token. Usually the username, the access key or the Partner-Id are wrong.
/// </summary>
public sealed class SiigoAuthenticationException : SiigoApiException
{
    /// <summary>Creates the exception.</summary>
    public SiigoAuthenticationException(
        string message,
        HttpStatusCode statusCode,
        IReadOnlyList<SiigoError>? errors = null,
        string? responseBody = null)
        : base(message, statusCode, errors, responseBody)
    {
    }
}
