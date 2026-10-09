using System.Net;

namespace SiigoSharp.Internal;

/// <summary>
/// The texts of the exceptions thrown by the library, in Spanish and English.
/// Both translations of a message sit side by side so they stay in sync.
/// </summary>
internal sealed class Messages
{
    private readonly bool _spanish;

    public Messages(SiigoLanguage language)
    {
        Language = language;
        _spanish = language != SiigoLanguage.English;
    }

    public SiigoLanguage Language { get; }

    public string OptionRequired(string name) => _spanish
        ? $"La opción '{name}' es obligatoria."
        : $"The '{name}' option is required.";

    public string OptionMustBeAbsoluteUrl(string name) => _spanish
        ? $"La opción '{name}' debe ser una URL absoluta."
        : $"The '{name}' option must be an absolute URL.";

    public string OptionMustBePositive(string name) => _spanish
        ? $"La opción '{name}' debe ser mayor que cero."
        : $"The '{name}' option must be greater than zero.";

    public string OptionMustNotBeNegative(string name) => _spanish
        ? $"La opción '{name}' no puede ser negativa."
        : $"The '{name}' option cannot be negative.";

    public string PartnerIdFormat => _spanish
        ? "La opción 'PartnerId' debe tener entre 3 y 100 letras o números, sin espacios ni caracteres especiales."
        : "The 'PartnerId' option must have 3 to 100 letters or digits, without spaces or special characters.";

    public string IdempotencyKeyFormat => _spanish
        ? "La clave de idempotencia debe tener entre 1 y 30 letras o números, sin espacios ni caracteres especiales."
        : "The idempotency key must have 1 to 30 letters or digits, without spaces or special characters.";

    public string TaxNotInCatalog(int taxId) => _spanish
        ? $"El impuesto {taxId} no está en el catálogo de impuestos recibido."
        : $"Tax {taxId} is not in the given tax catalog.";

    public string TaxNotSupportedForTotals(string? name, string? type) => _spanish
        ? $"InvoiceTotals solo calcula impuestos de tipo IVA e Impoconsumo; '{name}' es de tipo '{type}'."
        : $"InvoiceTotals only computes IVA and Impoconsumo taxes; '{name}' is of type '{type}'.";

    public string PercentageDiscountsNotSupported(string? documentType) => _spanish
        ? $"InvoiceTotals solo calcula descuentos en valor; el tipo de documento '{documentType}' los expresa en porcentaje."
        : $"InvoiceTotals only computes discounts given as values; document type '{documentType}' gives them as percentages.";

    public string AuthenticationFailed(HttpStatusCode status, IReadOnlyList<SiigoError> errors) => _spanish
        ? $"Siigo rechazó la autenticación (HTTP {Status(status)}). Verifique Username, AccessKey y PartnerId.{Details(errors)}"
        : $"Siigo rejected the authentication (HTTP {Status(status)}). Check Username, AccessKey and PartnerId.{Details(errors)}";

    public string TokenMissing => _spanish
        ? "La respuesta de autenticación de Siigo no incluye un access_token."
        : "The authentication response from Siigo does not include an access_token.";

    public string ApiError(HttpMethod method, Uri? uri, HttpStatusCode status, IReadOnlyList<SiigoError> errors) => _spanish
        ? $"Siigo respondió HTTP {Status(status)} a {method} {PathOf(uri)}.{Details(errors)}"
        : $"Siigo returned HTTP {Status(status)} for {method} {PathOf(uri)}.{Details(errors)}";

    public string EmptyResponse(HttpMethod method, Uri? uri) => _spanish
        ? $"Siigo devolvió una respuesta vacía a {method} {PathOf(uri)}."
        : $"Siigo returned an empty response for {method} {PathOf(uri)}.";

    public string InvalidResponse(HttpMethod method, Uri? uri) => _spanish
        ? $"No se pudo leer la respuesta de Siigo a {method} {PathOf(uri)}: el JSON no tiene el formato esperado."
        : $"Could not read the response from Siigo for {method} {PathOf(uri)}: the JSON does not have the expected shape.";

    private static string Status(HttpStatusCode status) => $"{(int)status} {status}";

    private static string Details(IReadOnlyList<SiigoError> errors) =>
        errors.Count == 0 ? string.Empty : " " + string.Join(" ", errors.Select(error => error.ToString()));

    // Only the path: query strings may carry customer identifications, and these messages end up in logs.
    private static string PathOf(Uri? uri) => uri switch
    {
        null => string.Empty,
        { IsAbsoluteUri: true } => uri.AbsolutePath,
        _ => "/" + uri.OriginalString.Split('?')[0].TrimStart('/'),
    };
}
