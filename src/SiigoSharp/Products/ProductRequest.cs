namespace SiigoSharp;

/// <summary>
/// The data to create a product, or to replace one with <see cref="IProductsClient.UpdateAsync"/>.
/// </summary>
/// <example>
/// <code>
/// var request = new ProductRequest
/// {
///     Code = "TORN-001",
///     Name = "Tornillo drywall 6x1",
///     AccountGroup = accountGroup.Id,
///     Taxes = [new IdReference(iva19.Id)],
///     Prices = [ProductPrice.Create(350m)],
/// };
/// </code>
/// </example>
public sealed class ProductRequest
{
    /// <summary>The unique product code (SKU): up to 30 characters, without spaces.</summary>
    public required string Code { get; set; }

    /// <summary>The name, up to 100 characters.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// The id of the inventory group (account group); see <see cref="ICatalogsClient.GetAccountGroupsAsync"/>.
    /// It cannot change once the product has movements.
    /// </summary>
    public required int AccountGroup { get; set; }

    /// <summary>Product (Siigo's default), service, consumer good or combo.</summary>
    public ProductType? Type { get; set; }

    /// <summary>Whether Siigo tracks the stock of the product.</summary>
    public bool? StockControl { get; set; }

    /// <summary>Whether the product is active.</summary>
    public bool? Active { get; set; }

    /// <summary>How VAT applies; Siigo's default is <see cref="SiigoSharp.TaxClassification.Taxed"/>.</summary>
    public TaxClassification? TaxClassification { get; set; }

    /// <summary>Whether the prices include taxes.</summary>
    public bool? TaxIncluded { get; set; }

    /// <summary>The consumption tax value, if any.</summary>
    public decimal? TaxConsumptionValue { get; set; }

    /// <summary>The taxes, by id; see <see cref="ICatalogsClient.GetTaxesAsync"/>.</summary>
    public IList<IdReference>? Taxes { get; set; }

    /// <summary>The prices, by currency and price list; see <see cref="ProductPrice.Create"/>.</summary>
    public IList<ProductPrice>? Prices { get; set; }

    /// <summary>The DIAN unit of measure code. Siigo uses <c>"94"</c> (unit) by default.</summary>
    public string? Unit { get; set; }

    /// <summary>The label of the unit of measure shown on documents.</summary>
    public string? UnitLabel { get; set; }

    /// <summary>The manufacturer reference.</summary>
    public string? Reference { get; set; }

    /// <summary>The description.</summary>
    public string? Description { get; set; }

    /// <summary>Barcode, brand, tariff heading and model.</summary>
    public ProductAdditionalFields? AdditionalFields { get; set; }
}
