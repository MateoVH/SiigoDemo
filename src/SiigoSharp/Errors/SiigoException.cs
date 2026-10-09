namespace SiigoSharp;

/// <summary>
/// Base class of the errors raised by SiigoSharp, for example when a response from Siigo cannot be read.
/// </summary>
public class SiigoException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public SiigoException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the exception that caused it.</summary>
    public SiigoException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
