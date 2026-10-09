namespace SiigoSharp;

/// <summary>
/// An error reported by Siigo in the body of a failed response.
/// </summary>
public sealed class SiigoError
{
    /// <summary>Creates an error.</summary>
    public SiigoError(string? code, string? message, IReadOnlyList<string>? parameters = null, string? detail = null)
    {
        Code = code;
        Message = message;
        Params = parameters ?? [];
        Detail = detail;
    }

    /// <summary>The error code, such as <c>already_exists</c> or <c>invalid_reference</c>.</summary>
    public string? Code { get; }

    /// <summary>The description written by Siigo.</summary>
    public string? Message { get; }

    /// <summary>The fields the error refers to, such as <c>identification</c>.</summary>
    public IReadOnlyList<string> Params { get; }

    /// <summary>Additional detail, when Siigo provides it.</summary>
    public string? Detail { get; }

    /// <summary>Formats the error as <c>[code] message (params)</c>.</summary>
    public override string ToString()
    {
        var text = Code is null ? Message ?? string.Empty : $"[{Code}] {Message}".TrimEnd();
        return Params.Count == 0 ? text : $"{text} ({string.Join(", ", Params)})";
    }
}
