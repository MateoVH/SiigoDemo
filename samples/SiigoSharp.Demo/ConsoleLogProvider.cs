using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace SiigoSharp.Demo;

/// <summary>
/// Shows what the library does behind the scenes (token renewals, retries) as styled console lines,
/// and counts those events for the final summary.
/// </summary>
internal sealed class ConsoleLogProvider : ILoggerProvider
{
    private int _tokensAcquired;
    private int _retries;

    /// <summary>Tokens obtained after the first one.</summary>
    public int TokensRenewed => Math.Max(0, Volatile.Read(ref _tokensAcquired) - 1);

    public int Retries => Volatile.Read(ref _retries);

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose()
    {
    }

    private void Write(EventId eventId, string message)
    {
        var (icon, color) = eventId.Name switch
        {
            "TokenAcquired" => ("🔑", "green"),
            "TokenRejected" => ("🔄", "yellow"),
            "Retrying" => ("⏳", "yellow"),
            "RetryAfterTooLong" => ("⚠️", "red"),
            _ => ("·", "grey"),
        };

        switch (eventId.Name)
        {
            case "TokenAcquired":
                Interlocked.Increment(ref _tokensAcquired);
                break;
            case "Retrying":
                Interlocked.Increment(ref _retries);
                break;
        }

        AnsiConsole.MarkupLine($"   {icon} [{color}]{Markup.Escape(message)}[/]");
    }

    private sealed class Logger(ConsoleLogProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Information && category.StartsWith("SiigoSharp", StringComparison.Ordinal);

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(eventId, formatter(state, exception));
            }
        }
    }
}
