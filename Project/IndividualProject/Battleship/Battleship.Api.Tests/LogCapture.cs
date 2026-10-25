using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Battleship.Api.Tests;

public sealed class LogCapture : ILoggerProvider, ILogger
{
    private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => _entries.ToArray();

    public IReadOnlyCollection<string> Messages => _entries.Select(entry => entry.Message).ToArray();

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _entries.Enqueue((logLevel, formatter(state, exception)));
    }

    public void Dispose()
    {
    }
}
