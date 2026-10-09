namespace SiigoSharp;

/// <summary>Paging parameters shared by the list operations.</summary>
public abstract record SiigoQuery
{
    /// <summary>The page to read, starting at 1. Siigo returns the first page when it is not set.</summary>
    public int? Page { get; init; }

    /// <summary>The number of results per page. Siigo returns 25 when it is not set.</summary>
    public int? PageSize { get; init; }
}
