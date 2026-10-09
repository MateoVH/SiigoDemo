namespace SiigoSharp;

/// <summary>
/// The language of the messages written by the library: exception messages and log entries.
/// Errors reported by Siigo itself (<see cref="SiigoError"/>) keep the wording Siigo uses.
/// </summary>
public enum SiigoLanguage
{
    /// <summary>Spanish (default).</summary>
    Spanish = 0,

    /// <summary>English.</summary>
    English = 1,
}
