using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Battleship.Api.Tests;

public sealed record LogEntry(
    LogEventLevel Level,
    string? EventName,
    string Message,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception = null,
    string? TraceId = null);

/// <summary>
/// A Serilog sink that keeps every event in memory, so tests can read what was logged.
/// </summary>
public sealed class LogCapture : ILogEventSink
{
    private const string GameCodeProperty = "GameCode";
    private const string EventNameProperty = "EventName";

    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

    public void Clear()
    {
        _entries.Clear();
    }

    public IReadOnlyCollection<LogEntry> ForGame(string code)
    {
        return Entries.Where(entry => Equals(entry.Properties.GetValueOrDefault(GameCodeProperty), code)).ToArray();
    }

    public void Emit(LogEvent logEvent)
    {
        var properties = logEvent.Properties.ToDictionary(property => property.Key, property => Plain(property.Value));
        _entries.Enqueue(new LogEntry(
            logEvent.Level,
            properties.GetValueOrDefault(EventNameProperty) as string ?? FrameworkEventName(properties),
            logEvent.RenderMessage(),
            properties,
            logEvent.Exception,
            logEvent.TraceId?.ToString()));
    }

    // Logs written by the framework carry an EventId ({ Id, Name }) instead of our EventName.
    private static string? FrameworkEventName(IReadOnlyDictionary<string, object?> properties)
    {
        return properties.GetValueOrDefault("EventId") is IReadOnlyDictionary<string, object?> eventId
            ? eventId.GetValueOrDefault("Name") as string
            : null;
    }

    private static object? Plain(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            StructureValue structure => structure.Properties.ToDictionary(property => property.Name, property => Plain(property.Value)),
            SequenceValue sequence => sequence.Elements.Select(Plain).ToList(),
            DictionaryValue dictionary => dictionary.Elements.ToDictionary(element => element.Key.Value ?? string.Empty, element => Plain(element.Value)),
            _ => value.ToString(),
        };
    }
}
