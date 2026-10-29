using System.Diagnostics;
using Battleship.Core;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Battleship.Api.Tests;

public class LogCaptureTests
{
    private LogCapture _capture = null!;
    private Logger _logger = null!;

    [SetUp]
    public void CreateLogger()
    {
        _capture = new LogCapture();
        _logger = new LoggerConfiguration().WriteTo.Sink(_capture).CreateLogger();
    }

    [TearDown]
    public void DisposeLogger()
    {
        _logger.Dispose();
    }

    [Test]
    public void Emit_StoresTheLevelAndTheRenderedMessage()
    {
        _logger.Warning("{PlayerName} won", "Ann");

        var entry = _capture.Entries.Single();
        Assert.That(entry.Level, Is.EqualTo(LogEventLevel.Warning));
        Assert.That(entry.Message, Is.EqualTo("\"Ann\" won"));
    }

    [Test]
    public void Emit_StoresTheEventNameProperty()
    {
        _logger.ForContext("EventName", "GameWon").Information("{PlayerName} won", "Ann");

        Assert.That(_capture.Entries.Single().EventName, Is.EqualTo("GameWon"));
    }

    [Test]
    public void Emit_UsesTheNameOfTheEventIdPropertyThatFrameworkLogsCarry()
    {
        _logger.ForContext("EventId", new { Id = 6, Name = "RequestRejected" }, destructureObjects: true).Information("rejected");

        var entry = _capture.Entries.Single();
        Assert.That(entry.EventName, Is.EqualTo("RequestRejected"));
    }

    [Test]
    public void Emit_WithoutAnEventName_LeavesItNull()
    {
        _logger.Information("{PlayerName} won", "Ann");

        Assert.That(_capture.Entries.Single().EventName, Is.Null);
    }

    [Test]
    public void Emit_StoresPropertiesAsPlainValues()
    {
        _logger.Information("{PlayerName} fired {Count} {Day}", "Ann", 3, DayOfWeek.Monday);

        var properties = _capture.Entries.Single().Properties;
        Assert.That(properties["PlayerName"], Is.EqualTo("Ann"));
        Assert.That(properties["Count"], Is.EqualTo(3));
        Assert.That(properties["Day"], Is.EqualTo(DayOfWeek.Monday));
    }

    [Test]
    public void Emit_StoresADestructuredObjectAsADictionary()
    {
        _logger.Information("fired at {@Target}", new Coordinate(2, 4));

        var target = (IReadOnlyDictionary<string, object?>)_capture.Entries.Single().Properties["Target"]!;
        Assert.That(target["X"], Is.EqualTo(2));
        Assert.That(target["Y"], Is.EqualTo(4));
    }

    [Test]
    public void Emit_StoresTheException()
    {
        _logger.Error(new InvalidOperationException("boom"), "failed");

        var exception = _capture.Entries.Single().Exception;
        Assert.That(exception, Is.InstanceOf<InvalidOperationException>());
        Assert.That(exception!.Message, Is.EqualTo("boom"));
    }

    [Test]
    public void Emit_StoresTheTraceIdOfTheCurrentActivity()
    {
        using var activity = new Activity("test").Start();

        _logger.Information("inside a request");

        Assert.That(_capture.Entries.Single().TraceId, Is.EqualTo(activity.TraceId.ToString()));
    }

    [Test]
    public void Emit_WithoutAnActivity_LeavesTheTraceIdNull()
    {
        Activity.Current = null;

        _logger.Information("outside a request");

        Assert.That(_capture.Entries.Single().TraceId, Is.Null);
    }

    [Test]
    public void ForGame_ReturnsOnlyTheEntriesWhoseGameCodeMatches()
    {
        _logger.ForContext("GameCode", "AAAAA").Information("one");
        _logger.ForContext("GameCode", "BBBBB").Information("two");
        _logger.Information("three");

        var entries = _capture.ForGame("AAAAA");

        Assert.That(entries, Has.Exactly(1).Items);
        Assert.That(entries.Single().Message, Is.EqualTo("one"));
    }
}
