using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiigoSharp.Internal;

/// <summary>
/// Enums as strings, read case-insensitively. In responses enum properties are nullable, and a value
/// this version does not know (say, a product type Siigo adds later) becomes <c>null</c> instead of
/// failing the whole response.
/// </summary>
internal sealed class LenientEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsEnum || Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert);
        var converterType = underlying is null
            ? typeof(EnumConverter<>).MakeGenericType(typeToConvert)
            : typeof(NullableEnumConverter<>).MakeGenericType(underlying);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private static bool TryRead<T>(ref Utf8JsonReader reader, out T value)
        where T : struct, Enum
    {
        if (reader.TokenType == JsonTokenType.String &&
            Enum.TryParse(reader.GetString(), ignoreCase: true, out value) &&
            Enum.IsDefined(value))
        {
            return true;
        }

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            reader.Skip();
        }

        value = default;
        return false;
    }

    private sealed class EnumConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            TryRead(ref reader, out T value) ? value : throw new JsonException($"Unknown {typeof(T).Name} value.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }

    private sealed class NullableEnumConverter<T> : JsonConverter<T?>
        where T : struct, Enum
    {
        public override bool HandleNull => true;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType != JsonTokenType.Null && TryRead(ref reader, out T value) ? value : null;

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value is { } enumValue)
            {
                writer.WriteStringValue(enumValue.ToString());
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}

/// <summary>Reads a JSON <c>null</c> list as an empty one, so response collections are never null.</summary>
internal sealed class EmptyListForNullConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class Converter<T> : JsonConverter<IReadOnlyList<T>>
    {
        public override bool HandleNull => true;

        public override IReadOnlyList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? [] : JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? [];

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<T> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.AsEnumerable(), options);
    }
}

/// <summary>Dates as <c>yyyy-MM-dd</c>; when reading, a full date-time is accepted too.</summary>
internal sealed class DateOnlyConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime.DateTime);
        }

        throw new JsonException($"'{text}' is not a valid date.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}

/// <summary>
/// For text fields whose shape Siigo does not document precisely: a string is kept as is, and any
/// other JSON value is kept as its raw JSON text.
/// </summary>
internal sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
