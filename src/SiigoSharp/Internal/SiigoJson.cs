using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SiigoSharp.Internal;

/// <summary>
/// JSON conventions of the Siigo API: snake_case names, enums as strings and no <c>null</c> fields in requests.
/// </summary>
internal static class SiigoJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>
    /// Serializes <paramref name="value"/> into a buffered body. Unlike a streamed body, it has a known
    /// Content-Length and can be sent again when a request is retried.
    /// </summary>
    public static ByteArrayContent CreateContent<T>(T value)
    {
        var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(value, Options));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        // Web defaults: case-insensitive names and numbers accepted when they arrive as strings.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ShapeInvoiceRequest } },
        };
        options.Converters.Add(new LenientEnumConverterFactory());
        options.Converters.Add(new EmptyListForNullConverterFactory());
        options.Converters.Add(new DateOnlyConverter());
        return options;
    }

    /// <summary>
    /// <see cref="InvoiceRequest"/> is a plain DTO, so it survives being saved and loaded again with any
    /// serializer (for example in an outbox table). Only on its way to Siigo does it take Siigo's shape:
    /// <c>SendToDian</c> and <c>SendByEmail</c> become <c>stamp.send</c> and <c>mail.send</c>, and
    /// <c>IdempotencyKey</c> leaves the body because it travels as a header.
    /// </summary>
    private static void ShapeInvoiceRequest(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(InvoiceRequest))
        {
            return;
        }

        var properties = typeInfo.Properties;
        for (var i = properties.Count - 1; i >= 0; i--)
        {
            if (properties[i].AttributeProvider is PropertyInfo
                {
                    Name: nameof(InvoiceRequest.SendToDian) or nameof(InvoiceRequest.SendByEmail) or nameof(InvoiceRequest.IdempotencyKey),
                })
            {
                properties.RemoveAt(i);
            }
        }

        properties.Add(SendFlag(typeInfo, "stamp", invoice => invoice.SendToDian));
        properties.Add(SendFlag(typeInfo, "mail", invoice => invoice.SendByEmail));
    }

    private static JsonPropertyInfo SendFlag(JsonTypeInfo typeInfo, string name, Func<InvoiceRequest, bool> send)
    {
        var property = typeInfo.CreateJsonPropertyInfo(typeof(SendOption), name);
        property.Get = invoice => new SendOption(send((InvoiceRequest)invoice));
        return property;
    }

    private sealed record SendOption(bool Send);
}
