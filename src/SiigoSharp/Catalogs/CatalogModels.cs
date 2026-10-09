using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiigoSharp;

/// <summary>Document type codes used to filter catalogs.</summary>
public static class DocumentTypeCodes
{
    /// <summary>Factura de venta (sales invoice).</summary>
    public const string SalesInvoice = "FV";

    /// <summary>Nota crédito (credit note).</summary>
    public const string CreditNote = "NC";

    /// <summary>Recibo de caja (cash receipt).</summary>
    public const string CashReceipt = "RC";
}

/// <summary>Tax types reported by Siigo in <see cref="Tax.Type"/>.</summary>
public static class TaxTypes
{
    /// <summary>Value added tax.</summary>
    public const string Iva = "IVA";

    /// <summary>Consumption tax.</summary>
    public const string Impoconsumo = "Impoconsumo";

    /// <summary>Withholding tax.</summary>
    public const string Retefuente = "Retefuente";

    /// <summary>VAT withholding.</summary>
    public const string ReteIva = "ReteIVA";

    /// <summary>Industry and commerce tax withholding.</summary>
    public const string ReteIca = "ReteICA";

    /// <summary>Ad valorem tax, such as the sugary drinks tax.</summary>
    public const string AdValorem = "AdValorem";

    /// <summary>Self-withholding.</summary>
    public const string Autorretencion = "Autorretencion";
}

/// <summary>A document type configured in the company, e.g. its electronic sales invoice.</summary>
public sealed class DocumentType
{
    /// <summary>The id, used as <see cref="InvoiceRequest.Document"/>.</summary>
    public int Id { get; init; }

    /// <summary>The code within the company.</summary>
    public string? Code { get; init; }

    /// <summary>The name.</summary>
    public string? Name { get; init; }

    /// <summary>The description.</summary>
    public string? Description { get; init; }

    /// <summary>The document class: FV, NC, RC...</summary>
    public string? Type { get; init; }

    /// <summary>Whether the document type is active.</summary>
    public bool? Active { get; init; }

    /// <summary>Whether each item can have its own seller.</summary>
    public bool? SellerByItem { get; init; }

    /// <summary>Whether documents of this type use cost centers.</summary>
    public bool? CostCenter { get; init; }

    /// <summary>Whether a cost center is mandatory.</summary>
    public bool? CostCenterMandatory { get; init; }

    /// <summary>Whether Siigo numbers the documents; otherwise the invoice must carry a number.</summary>
    public bool? AutomaticNumber { get; init; }

    /// <summary>The next consecutive number.</summary>
    public long? Consecutive { get; init; }

    /// <summary>Whether item discounts are a <c>"Percentage"</c> or a <c>"Value"</c>.</summary>
    public string? DiscountType { get; init; }

    /// <summary>Whether amounts may have decimals.</summary>
    public bool? Decimals { get; init; }

    /// <summary>The kind of electronic document, e.g. <c>"NoElectronic"</c>.</summary>
    public string? ElectronicType { get; init; }

    /// <summary>Fields returned by Siigo that this version of the library does not map.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>A tax configured in the company.</summary>
public sealed class Tax
{
    /// <summary>The id, used in item and product taxes.</summary>
    public int Id { get; init; }

    /// <summary>The name, e.g. <c>"IVA 19%"</c>.</summary>
    public string? Name { get; init; }

    /// <summary>The type, e.g. <c>"IVA"</c>; see <see cref="TaxTypes"/>.</summary>
    public string? Type { get; init; }

    /// <summary>The rate, e.g. <c>19</c>.</summary>
    public decimal? Percentage { get; init; }

    /// <summary>Whether the tax is active.</summary>
    public bool? Active { get; init; }
}

/// <summary>A payment method configured in the company.</summary>
public sealed class PaymentType
{
    /// <summary>The id, used in <see cref="InvoicePaymentRequest.Id"/>.</summary>
    public int Id { get; init; }

    /// <summary>The name, e.g. <c>"Efectivo"</c> or <c>"Crédito"</c>.</summary>
    public string? Name { get; init; }

    /// <summary>The type: <c>"Cartera"</c>, <c>"Proveedor"</c> or <c>"CarteraProveedor"</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Whether the payment method is active.</summary>
    public bool? Active { get; init; }

    /// <summary>Whether payments of this type need a due date.</summary>
    public bool? DueDate { get; init; }
}

/// <summary>A Siigo user; invoices use one as seller.</summary>
public sealed class SiigoUser
{
    /// <summary>The id, used as <see cref="InvoiceRequest.Seller"/>.</summary>
    public int Id { get; init; }

    /// <summary>The user name.</summary>
    public string? Username { get; init; }

    /// <summary>The first name.</summary>
    public string? FirstName { get; init; }

    /// <summary>The last name.</summary>
    public string? LastName { get; init; }

    /// <summary>The e-mail address.</summary>
    public string? Email { get; init; }

    /// <summary>Whether the user is active.</summary>
    public bool? Active { get; init; }

    /// <summary>The identification number.</summary>
    public string? Identification { get; init; }

    /// <summary>First and last names.</summary>
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>An inventory group (account group); every product belongs to one.</summary>
public sealed class AccountGroup
{
    /// <summary>The id, used as <see cref="ProductRequest.AccountGroup"/>.</summary>
    public int Id { get; init; }

    /// <summary>The name.</summary>
    public string? Name { get; init; }

    /// <summary>Whether the group is active.</summary>
    public bool? Active { get; init; }
}

/// <summary>A cost center.</summary>
public sealed class CostCenter
{
    /// <summary>The id, used as <see cref="InvoiceRequest.CostCenter"/>.</summary>
    public int Id { get; init; }

    /// <summary>The code.</summary>
    public string? Code { get; init; }

    /// <summary>The name.</summary>
    public string? Name { get; init; }

    /// <summary>Whether the cost center is active.</summary>
    public bool? Active { get; init; }
}
