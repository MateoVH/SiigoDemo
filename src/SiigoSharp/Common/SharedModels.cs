namespace SiigoSharp;

/// <summary>A reference to a Siigo record by its numeric id, serialized as <c>{ "id": 123 }</c>.</summary>
/// <param name="Id">The id of the record (document type, tax, payment type...).</param>
public sealed record IdReference(int Id);

/// <summary>A code with its description, as Siigo returns catalogs such as <c>id_type</c> or <c>unit</c>.</summary>
public sealed class CodeName
{
    /// <summary>The code, e.g. <c>"13"</c>.</summary>
    public string? Code { get; init; }

    /// <summary>The description, e.g. <c>"Cédula de ciudadanía"</c>.</summary>
    public string? Name { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Code} {Name}".Trim();
}

/// <summary>When a record was created and last updated in Siigo.</summary>
public sealed class ResourceMetadata
{
    /// <summary>The creation date.</summary>
    public DateTimeOffset? Created { get; init; }

    /// <summary>The date of the last update, if any.</summary>
    public DateTimeOffset? LastUpdated { get; init; }

    /// <summary>The date of the last stock change, for products and invoices.</summary>
    public DateTimeOffset? StockUpdated { get; init; }
}
