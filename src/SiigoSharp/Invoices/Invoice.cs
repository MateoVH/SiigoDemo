using System.Text.Json;
using System.Text.Json.Serialization;
using SiigoSharp.Internal;

namespace SiigoSharp;

/// <summary>A sales invoice as stored in Siigo.</summary>
public sealed class Invoice
{
    /// <summary>The Siigo id (a GUID).</summary>
    public string? Id { get; init; }

    /// <summary>The document type.</summary>
    public IdReference? Document { get; init; }

    /// <summary>The prefix of the numbering, e.g. <c>"FV"</c>.</summary>
    public string? Prefix { get; init; }

    /// <summary>The consecutive number.</summary>
    public long? Number { get; init; }

    /// <summary>The full number, e.g. <c>"FV-1-22"</c>.</summary>
    public string? Name { get; init; }

    /// <summary>The invoice date.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>The customer.</summary>
    public InvoiceCustomer? Customer { get; init; }

    /// <summary>The cost center id.</summary>
    public int? CostCenter { get; init; }

    /// <summary>The currency, for invoices in a foreign currency.</summary>
    public InvoiceCurrency? Currency { get; init; }

    /// <summary>The total, taxes included.</summary>
    public decimal? Total { get; init; }

    /// <summary>The amount still owed.</summary>
    public decimal? Balance { get; init; }

    /// <summary>The seller id.</summary>
    public int? Seller { get; init; }

    /// <summary>The status of the electronic invoice before the DIAN, and its CUFE once accepted.</summary>
    public InvoiceStamp? Stamp { get; init; }

    /// <summary>The status of the e-mail sent to the customer.</summary>
    public InvoiceMail? Mail { get; init; }

    /// <summary>The observations.</summary>
    public string? Observations { get; init; }

    /// <summary>The items.</summary>
    public IReadOnlyList<InvoiceItem> Items { get; init; } = [];

    /// <summary>The payments.</summary>
    public IReadOnlyList<InvoicePayment> Payments { get; init; } = [];

    /// <summary>A public link to view the invoice.</summary>
    public string? PublicUrl { get; init; }

    /// <summary>Whether the invoice was annulled.</summary>
    public bool? Annulled { get; init; }

    /// <summary>Creation and update dates.</summary>
    public ResourceMetadata? Metadata { get; init; }

    /// <summary>Fields returned by Siigo that this version of the library does not map.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Values of <see cref="InvoiceStamp.Status"/>.</summary>
public static class InvoiceStampStatus
{
    /// <summary>Not sent to the DIAN yet.</summary>
    public const string Draft = "Draft";

    /// <summary>Accepted by the DIAN; the invoice has a CUFE.</summary>
    public const string Accepted = "Accepted";

    /// <summary>Rejected by the DIAN.</summary>
    public const string Rejected = "Rejected";
}

/// <summary>The customer of an invoice.</summary>
public sealed class InvoiceCustomer
{
    /// <summary>The Siigo id of the customer.</summary>
    public string? Id { get; init; }

    /// <summary>The identification number.</summary>
    public string? Identification { get; init; }

    /// <summary>The branch office.</summary>
    public int? BranchOffice { get; init; }
}

/// <summary>The electronic invoicing status of an invoice (field <c>stamp</c>).</summary>
public sealed class InvoiceStamp
{
    /// <summary>The status: <c>Draft</c>, <c>Accepted</c> or <c>Rejected</c>; see <see cref="InvoiceStampStatus"/>.</summary>
    public string? Status { get; init; }

    /// <summary>The CUFE, the unique code the DIAN assigns to an accepted electronic invoice.</summary>
    public string? Cufe { get; init; }

    /// <summary>The CUDE, when applicable.</summary>
    public string? Cude { get; init; }

    /// <summary>Observations from the DIAN.</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Observations { get; init; }

    /// <summary>The errors reported by the DIAN, as Siigo returns them.</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Errors { get; init; }
}

/// <summary>The status of the invoice e-mail (field <c>mail</c>).</summary>
public sealed class InvoiceMail
{
    /// <summary>The status, e.g. <c>not_sent</c>, <c>sent</c>, <c>read</c> or <c>failed_delivery</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Observations about the delivery.</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Observations { get; init; }
}

/// <summary>An item of an invoice.</summary>
public sealed class InvoiceItem
{
    /// <summary>The id of the item.</summary>
    public string? Id { get; init; }

    /// <summary>The product code.</summary>
    public string? Code { get; init; }

    /// <summary>The description.</summary>
    public string? Description { get; init; }

    /// <summary>The quantity.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>The unit price.</summary>
    public decimal? Price { get; init; }

    /// <summary>The discount, as a percentage and as a value.</summary>
    public InvoiceItemDiscount? Discount { get; init; }

    /// <summary>The taxes with their computed values.</summary>
    public IReadOnlyList<InvoiceItemTax> Taxes { get; init; } = [];

    /// <summary>The total of the item, taxes included.</summary>
    public decimal? Total { get; init; }
}

/// <summary>The discount of an invoice item.</summary>
[JsonConverter(typeof(InvoiceItemDiscountConverter))]
public sealed class InvoiceItemDiscount
{
    /// <summary>The discount as a percentage.</summary>
    public decimal? Percentage { get; init; }

    /// <summary>The discount as a value.</summary>
    public decimal? Value { get; init; }
}

/// <summary>A tax applied to an invoice item.</summary>
public sealed class InvoiceItemTax
{
    /// <summary>The tax id.</summary>
    public int Id { get; init; }

    /// <summary>The name, e.g. <c>"IVA 19%"</c>.</summary>
    public string? Name { get; init; }

    /// <summary>The type, e.g. <c>"IVA"</c>.</summary>
    public string? Type { get; init; }

    /// <summary>The rate.</summary>
    public decimal? Percentage { get; init; }

    /// <summary>The tax amount.</summary>
    public decimal? Value { get; init; }

    /// <summary>The amount the tax was computed on.</summary>
    public decimal? BaseValue { get; init; }
}

/// <summary>A payment of an invoice.</summary>
public sealed class InvoicePayment
{
    /// <summary>The payment method id.</summary>
    public int Id { get; init; }

    /// <summary>The payment method name.</summary>
    public string? Name { get; init; }

    /// <summary>The amount.</summary>
    public decimal? Value { get; init; }

    /// <summary>The due date.</summary>
    public DateOnly? DueDate { get; init; }
}

/// <summary>
/// Siigo documents the item discount as an object (<c>{ "percentage", "value" }</c>); a plain number is
/// accepted too and read as the value.
/// </summary>
internal sealed class InvoiceItemDiscountConverter : JsonConverter<InvoiceItemDiscount>
{
    public override InvoiceItemDiscount? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new InvoiceItemDiscount { Value = reader.GetDecimal() };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return null;
        }

        decimal? percentage = null;
        decimal? value = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString();
            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                reader.Skip();
            }
            else if (string.Equals(name, "percentage", StringComparison.OrdinalIgnoreCase))
            {
                percentage = reader.GetDecimal();
            }
            else if (string.Equals(name, "value", StringComparison.OrdinalIgnoreCase))
            {
                value = reader.GetDecimal();
            }
        }

        return new InvoiceItemDiscount { Percentage = percentage, Value = value };
    }

    public override void Write(Utf8JsonWriter writer, InvoiceItemDiscount value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Percentage is { } percentage)
        {
            writer.WriteNumber("percentage", percentage);
        }

        if (value.Value is { } amount)
        {
            writer.WriteNumber("value", amount);
        }

        writer.WriteEndObject();
    }
}
