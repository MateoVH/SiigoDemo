using System.Globalization;
using System.Text;

namespace SiigoSharp.Internal;

/// <summary>Builds a URL query string, skipping parameters without a value.</summary>
internal sealed class QueryStringBuilder
{
    private readonly StringBuilder _query = new();

    public QueryStringBuilder Add(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _query.Append(_query.Length == 0 ? '?' : '&')
                .Append(Uri.EscapeDataString(name))
                .Append('=')
                .Append(Uri.EscapeDataString(value));
        }

        return this;
    }

    public QueryStringBuilder Add(string name, int? value) =>
        Add(name, value?.ToString(CultureInfo.InvariantCulture));

    public QueryStringBuilder Add(string name, DateOnly? value) =>
        Add(name, value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    // Siigo accepts filter instants in UTC: yyyy-MM-ddTHH:mm:ssZ.
    public QueryStringBuilder Add(string name, DateTimeOffset? value) =>
        Add(name, value?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));

    public QueryStringBuilder AddPaging(SiigoQuery query) =>
        Add("page", query.Page).Add("page_size", query.PageSize);

    public string AppendTo(string path) => path + _query;
}
