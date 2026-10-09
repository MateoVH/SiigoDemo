namespace SiigoSharp.Internal;

/// <summary>
/// The options a client registered with dependency injection uses: validated and copied once, so every
/// component sees the same values for the lifetime of the application.
/// </summary>
internal sealed record SiigoOptionsSnapshot(SiigoClientOptions Value);
