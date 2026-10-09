namespace SiigoSharp;

/// <summary>Filters for <see cref="IProductsClient.ListAsync"/>. Unset filters are not sent.</summary>
public sealed record ProductQuery : SiigoQuery
{
    /// <summary>The product code.</summary>
    public string? Code { get; init; }

    /// <summary>Created at or after this instant.</summary>
    public DateTimeOffset? CreatedStart { get; init; }

    /// <summary>Created at or before this instant.</summary>
    public DateTimeOffset? CreatedEnd { get; init; }

    /// <summary>Updated at or after this instant. Stock changes count as updates.</summary>
    public DateTimeOffset? UpdatedStart { get; init; }

    /// <summary>Updated at or before this instant.</summary>
    public DateTimeOffset? UpdatedEnd { get; init; }
}
