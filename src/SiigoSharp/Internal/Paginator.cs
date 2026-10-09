using System.Runtime.CompilerServices;

namespace SiigoSharp.Internal;

internal static class Paginator
{
    /// <summary>Reads pages one after another, starting at <paramref name="firstPage"/>, until the last one.</summary>
    public static async IAsyncEnumerable<T> EnumerateAsync<T>(
        int firstPage,
        Func<int, CancellationToken, Task<SiigoPage<T>>> fetchPage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var page = Math.Max(firstPage, 1); ; page++)
        {
            var result = await fetchPage(page, cancellationToken).ConfigureAwait(false);
            foreach (var item in result.Results)
            {
                yield return item;
            }

            if (IsLastPage(page, result))
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Decides with the page number that was requested rather than the one Siigo echoes back, so a response
    /// that ignores the page parameter cannot keep the loop running forever.
    /// </summary>
    public static bool IsLastPage<T>(int requestedPage, SiigoPage<T> result)
    {
        var pageSize = result.Pagination.PageSize > 0 ? result.Pagination.PageSize : result.Results.Count;
        return result.Results.Count == 0 || (long)requestedPage * pageSize >= result.Pagination.TotalResults;
    }
}
