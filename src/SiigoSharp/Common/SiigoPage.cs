namespace SiigoSharp;

/// <summary>A page of results from a Siigo list endpoint.</summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed class SiigoPage<T>
{
    /// <summary>The position of this page and the total number of results.</summary>
    public SiigoPagination Pagination { get; init; } = new();

    /// <summary>The items of this page.</summary>
    public IReadOnlyList<T> Results { get; init; } = [];

    /// <summary>Whether there are more results after this page.</summary>
    public bool HasNextPage =>
        Results.Count > 0 && Pagination.Page * Pagination.PageSize < Pagination.TotalResults;
}

/// <summary>Paging information of a <see cref="SiigoPage{T}"/>.</summary>
public sealed class SiigoPagination
{
    /// <summary>The page number, starting at 1.</summary>
    public int Page { get; init; }

    /// <summary>The maximum number of results per page.</summary>
    public int PageSize { get; init; }

    /// <summary>The number of results across all pages.</summary>
    public int TotalResults { get; init; }
}
