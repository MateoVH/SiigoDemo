using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SiigoSharp.Tests.Infrastructure;

internal sealed record LogEntry(string Category, LogLevel Level, EventId EventId, string Message);

/// <summary>Keeps every log entry in memory so tests can assert on them.</summary>
internal sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => [.. _entries];

    public ILoggerFactory CreateFactory() =>
        LoggerFactory.Create(builder => builder.AddProvider(this).SetMinimumLevel(LogLevel.Trace));

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(category, logLevel, eventId, formatter(state, exception)));
    }
}
