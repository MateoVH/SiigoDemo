using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiigoSharp;

/// <summary>A product or service as stored in Siigo.</summary>
public sealed class Product
{
    /// <summary>The Siigo id (a GUID).</summary>
    public string? Id { get; init; }

    /// <summary>The product code (SKU).</summary>
    public string? Code { get; init; }

    /// <summary>The name.</summary>
    public string? Name { get; init; }

    /// <summary>The inventory group the product belongs to.</summary>
    public AccountGroup? AccountGroup { get; init; }

    /// <summary>Product, service, consumer good or combo.</summary>
    public ProductType? Type { get; init; }

    /// <summary>Whether Siigo tracks the stock of the product.</summary>
    public bool? StockControl { get; init; }

    /// <summary>Whether the product is active.</summary>
    public bool? Active { get; init; }

    /// <summary>How VAT applies to the product.</summary>
    public TaxClassification? TaxClassification { get; init; }

    /// <summary>Whether the prices include taxes.</summary>
    public bool? TaxIncluded { get; init; }

    /// <summary>The consumption tax value, if any.</summary>
    public decimal? TaxConsumptionValue { get; init; }

    /// <summary>The taxes of the product.</summary>
    public IReadOnlyList<Tax> Taxes { get; init; } = [];

    /// <summary>The prices, by currency and price list.</summary>
    public IReadOnlyList<ProductPrice> Prices { get; init; } = [];

    /// <summary>The unit of measure, e.g. code <c>"94"</c>, name <c>"unidad"</c>.</summary>
    public CodeName? Unit { get; init; }

    /// <summary>The label of the unit of measure shown on documents.</summary>
    public string? UnitLabel { get; init; }

    /// <summary>The manufacturer reference.</summary>
    public string? Reference { get; init; }

    /// <summary>The description.</summary>
    public string? Description { get; init; }

    /// <summary>Barcode, brand, tariff heading and model.</summary>
    public ProductAdditionalFields? AdditionalFields { get; init; }

    /// <summary>The stock across all warehouses.</summary>
    public decimal? AvailableQuantity { get; init; }

    /// <summary>The stock per warehouse.</summary>
    public IReadOnlyList<ProductWarehouse> Warehouses { get; init; } = [];

    /// <summary>Creation, update and stock update dates.</summary>
    public ResourceMetadata? Metadata { get; init; }

    /// <summary>Fields returned by Siigo that this version of the library does not map.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>The prices of a product in one currency (field <c>prices</c>).</summary>
public sealed class ProductPrice
{
    /// <summary>The currency, e.g. <c>"COP"</c>.</summary>
    public string? CurrencyCode { get; set; }

    /// <summary>One price per price list, up to 12.</summary>
    public IList<PriceListEntry> PriceList { get; set; } = [];

    /// <summary>A single price in the first price list, e.g. <c>ProductPrice.Create(50_000m)</c>.</summary>
    public static ProductPrice Create(decimal value, string currencyCode = "COP") => new()
    {
        CurrencyCode = currencyCode,
        PriceList = [new PriceListEntry { Position = 1, Value = value }],
    };
}

/// <summary>A price in a price list.</summary>
public sealed class PriceListEntry
{
    /// <summary>The position of the price list, from 1 to 12.</summary>
    public int Position { get; set; }

    /// <summary>The name of the price list. Filled in by Siigo in responses.</summary>
    public string? Name { get; set; }

    /// <summary>The price.</summary>
    public decimal Value { get; set; }
}

/// <summary>Optional product data (field <c>additional_fields</c>).</summary>
public sealed class ProductAdditionalFields
{
    /// <summary>The barcode.</summary>
    public string? Barcode { get; set; }

    /// <summary>The brand.</summary>
    public string? Brand { get; set; }

    /// <summary>The customs tariff heading (partida arancelaria).</summary>
    public string? Tariff { get; set; }

    /// <summary>The model.</summary>
    public string? Model { get; set; }
}

/// <summary>The stock of a product in a warehouse.</summary>
public sealed class ProductWarehouse
{
    /// <summary>The warehouse id.</summary>
    public int Id { get; init; }

    /// <summary>The warehouse name.</summary>
    public string? Name { get; init; }

    /// <summary>The quantity in stock.</summary>
    public decimal? Quantity { get; init; }
}
