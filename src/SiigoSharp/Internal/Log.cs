using Microsoft.Extensions.Logging;

namespace SiigoSharp.Internal;

/// <summary>
/// The log entries written by the library. Event ids and property names are the same in every language,
/// so queries over structured logs work whatever language is configured; only the message text changes.
/// </summary>
internal static class Log
{
    private static readonly EventId RequestingTokenEvent = new(1, "RequestingToken");
    private static readonly EventId TokenAcquiredEvent = new(2, "TokenAcquired");
    private static readonly EventId TokenRejectedEvent = new(3, "TokenRejected");
    private static readonly EventId RetryingEvent = new(4, "Retrying");
    private static readonly EventId RetryAfterTooLongEvent = new(5, "RetryAfterTooLong");
    private static readonly EventId TokenRenewalFailedEvent = new(6, "TokenRenewalFailed");

    private static readonly Action<ILogger, Exception?> RequestingTokenEs = LoggerMessage.Define(
        LogLevel.Debug, RequestingTokenEvent, "Solicitando un token de acceso a Siigo");

    private static readonly Action<ILogger, Exception?> RequestingTokenEn = LoggerMessage.Define(
        LogLevel.Debug, RequestingTokenEvent, "Requesting a Siigo access token");

    private static readonly Action<ILogger, DateTimeOffset, Exception?> TokenAcquiredEs = LoggerMessage.Define<DateTimeOffset>(
        LogLevel.Information, TokenAcquiredEvent, "Token de acceso de Siigo obtenido; vence el {ExpiresAt:u}");

    private static readonly Action<ILogger, DateTimeOffset, Exception?> TokenAcquiredEn = LoggerMessage.Define<DateTimeOffset>(
        LogLevel.Information, TokenAcquiredEvent, "Siigo access token acquired; it expires on {ExpiresAt:u}");

    private static readonly Action<ILogger, string, string, Exception?> TokenRejectedEs = LoggerMessage.Define<string, string>(
        LogLevel.Warning, TokenRejectedEvent, "Siigo rechazó el token de acceso en {Method} {Path} (HTTP 401); se renueva y se repite la petición");

    private static readonly Action<ILogger, string, string, Exception?> TokenRejectedEn = LoggerMessage.Define<string, string>(
        LogLevel.Warning, TokenRejectedEvent, "Siigo rejected the access token on {Method} {Path} (HTTP 401); renewing it and repeating the request");

    // LoggerMessage binds arguments by position, so both translations keep the placeholders in the same order.
    private static readonly Action<ILogger, string, string, string, int, int, long, Exception?> RetryingEs =
        LoggerMessage.Define<string, string, string, int, int, long>(
            LogLevel.Warning, RetryingEvent, "{Method} {Path} falló ({Reason}); reintento {Attempt} de {MaxRetries} en {DelayMs} ms");

    private static readonly Action<ILogger, string, string, string, int, int, long, Exception?> RetryingEn =
        LoggerMessage.Define<string, string, string, int, int, long>(
            LogLevel.Warning, RetryingEvent, "{Method} {Path} failed ({Reason}); retry {Attempt} of {MaxRetries} in {DelayMs} ms");

    private static readonly Action<ILogger, double, string, string, double, Exception?> RetryAfterTooLongEs =
        LoggerMessage.Define<double, string, string, double>(
            LogLevel.Warning, RetryAfterTooLongEvent,
            "Siigo pidió esperar {RetryAfterSeconds} s antes de repetir {Method} {Path}, más que el máximo configurado ({MaxDelaySeconds} s); se devuelve el error");

    private static readonly Action<ILogger, double, string, string, double, Exception?> RetryAfterTooLongEn =
        LoggerMessage.Define<double, string, string, double>(
            LogLevel.Warning, RetryAfterTooLongEvent,
            "Siigo asked to wait {RetryAfterSeconds} s before repeating {Method} {Path}, longer than the configured maximum ({MaxDelaySeconds} s); returning the error");

    private static readonly Action<ILogger, Exception?> TokenRenewalFailedEs = LoggerMessage.Define(
        LogLevel.Warning, TokenRenewalFailedEvent,
        "No se pudo obtener un token de acceso de Siigo; si el actual sigue vigente, se seguirá usando");

    private static readonly Action<ILogger, Exception?> TokenRenewalFailedEn = LoggerMessage.Define(
        LogLevel.Warning, TokenRenewalFailedEvent,
        "Could not obtain a Siigo access token; the current one, if still valid, keeps being used");

    public static void TokenRenewalFailed(ILogger logger, SiigoLanguage language, Exception exception) =>
        Pick(language, TokenRenewalFailedEs, TokenRenewalFailedEn)(logger, exception);

    public static void RequestingToken(ILogger logger, SiigoLanguage language) =>
        Pick(language, RequestingTokenEs, RequestingTokenEn)(logger, null);

    public static void TokenAcquired(ILogger logger, SiigoLanguage language, DateTimeOffset expiresAt) =>
        Pick(language, TokenAcquiredEs, TokenAcquiredEn)(logger, expiresAt, null);

    public static void TokenRejected(ILogger logger, SiigoLanguage language, HttpRequestMessage request) =>
        Pick(language, TokenRejectedEs, TokenRejectedEn)(logger, request.Method.Method, PathOf(request), null);

    public static void Retrying(
        ILogger logger, SiigoLanguage language, HttpRequestMessage request, string reason, TimeSpan delay, int attempt, int maxRetries) =>
        Pick(language, RetryingEs, RetryingEn)(
            logger, request.Method.Method, PathOf(request), reason, attempt, maxRetries, (long)delay.TotalMilliseconds, null);

    public static void RetryAfterTooLong(
        ILogger logger, SiigoLanguage language, HttpRequestMessage request, TimeSpan retryAfter, TimeSpan maxDelay) =>
        Pick(language, RetryAfterTooLongEs, RetryAfterTooLongEn)(
            logger, retryAfter.TotalSeconds, request.Method.Method, PathOf(request), maxDelay.TotalSeconds, null);

    private static T Pick<T>(SiigoLanguage language, T spanish, T english) =>
        language == SiigoLanguage.English ? english : spanish;

    // Paths only: query strings may contain customer identifications.
    private static string PathOf(HttpRequestMessage request) =>
        request.RequestUri is { IsAbsoluteUri: true } uri ? uri.AbsolutePath : request.RequestUri?.OriginalString.Split('?')[0] ?? string.Empty;
}
