using System.Text.Json;

namespace SiigoSharp.Internal;

/// <summary>
/// Reads the errors from the body of a failed response. Siigo answers
/// <c>{ "Errors": [ { "Code", "Message", "Params", "Detail" } ] }</c>; the parser is lenient with casing
/// and with other shapes (a single error, or OAuth-style <c>error</c>/<c>error_description</c>), and
/// never throws: an unreadable body simply yields no errors.
/// </summary>
internal static class SiigoErrorParser
{
    public static IReadOnlyList<SiigoError> Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return ParseList(root);
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            if (TryGetProperty(root, "errors", out var errors))
            {
                return errors.ValueKind switch
                {
                    JsonValueKind.Array => ParseList(errors),
                    JsonValueKind.Object => [ParseError(errors)],
                    _ => [],
                };
            }

            var single = ParseError(root);
            return single.Code is null && single.Message is null ? [] : [single];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<SiigoError> ParseList(JsonElement array) =>
        array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(ParseError)
            .ToList();

    private static SiigoError ParseError(JsonElement error) => new(
        code: GetString(error, "code") ?? GetString(error, "error"),
        message: GetString(error, "message") ?? GetString(error, "error_description"),
        parameters: GetStrings(error, "params"),
        detail: GetString(error, "detail"));

    private static string? GetString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText(),
        };
    }

    private static List<string> GetStrings(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
        {
            return [];
        }

        return value.ValueKind switch
        {
            JsonValueKind.Array => value.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()! : item.GetRawText())
                .ToList(),
            JsonValueKind.String => [value.GetString()!],
            _ => [],
        };
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
