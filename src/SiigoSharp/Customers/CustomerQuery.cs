namespace SiigoSharp;

/// <summary>Filters for <see cref="ICustomersClient.ListAsync"/>. Unset filters are not sent.</summary>
public sealed record CustomerQuery : SiigoQuery
{
    /// <summary>The identification number.</summary>
    public string? Identification { get; init; }

    /// <summary>The branch office.</summary>
    public int? BranchOffice { get; init; }

    /// <summary>Created at or after this instant.</summary>
    public DateTimeOffset? CreatedStart { get; init; }

    /// <summary>Created at or before this instant.</summary>
    public DateTimeOffset? CreatedEnd { get; init; }

    /// <summary>Updated at or after this instant; useful to synchronize changes incrementally.</summary>
    public DateTimeOffset? UpdatedStart { get; init; }

    /// <summary>Updated at or before this instant.</summary>
    public DateTimeOffset? UpdatedEnd { get; init; }
}
