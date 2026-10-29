using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battleship.Api.Tests;

public sealed class FileLogApiFactory : ApiFactory
{
    private const string PathVariable = "Serilog__WriteTo__0__Args__path";

    private readonly bool _failToCreateGames;

    public FileLogApiFactory(string logPath, bool failToCreateGames = false)
    {
        _failToCreateGames = failToCreateGames;
        Environment.SetEnvironmentVariable(PathVariable, logPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable(PathVariable, null);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        if (_failToCreateGames)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<Func<Random>>();
                services.AddSingleton<Func<Random>>(_ => () => throw new InvalidOperationException("boom"));
            });
        }
    }
}

// The log path is passed through an environment variable, which every host in the process reads.
[NonParallelizable]
public class FileLogTests
{
    private string _directory = null!;

    private string LogPath => Path.Combine(_directory, "api-.log");

    [SetUp]
    public void CreateDirectory()
    {
        _directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"battleship-logs-{Guid.NewGuid():N}")).FullName;
    }

    [TearDown]
    public void DeleteDirectory()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private static string? Text(JsonElement entry, string name)
    {
        return entry.TryGetProperty(name, out var value) ? value.GetString() : null;
    }

    private static string? EventName(JsonElement entry)
    {
        return Text(entry, "EventName");
    }

    private async Task<(string Text, List<JsonElement> Entries)> ReadLogAsync()
    {
        var file = Directory.GetFiles(_directory, "api-*.log").Single();
        var text = await File.ReadAllTextAsync(file);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (text, lines.Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToList());
    }

    [Test]
    public async Task PlayedGame_IsWrittenAsOneJsonObjectPerLine_WithTheExpectedProperties()
    {
        GameClient ann;
        GameClient bob;
        var factory = new FileLogApiFactory(LogPath);
        await using (factory)
        {
            var http = factory.CreateClient();
            ann = await GameClient.CreateRoomAsync(http, "Ann");
            bob = await GameClient.JoinAsync(http, ann.Code, "Bob");
            await using var annHub = await HubClient.ConnectAsync(factory, ann);
            await ann.PlaceFleetAsync();
            await bob.PlaceFleetAsync();
            await ann.FireOkAsync(2, 4);
            await ann.GetAsync("wrong");
        }

        var (_, entries) = await ReadLogAsync();

        Assert.That(entries, Is.Not.Empty);
        Assert.That(entries.Select(entry => entry.TryGetProperty("@t", out _) && entry.TryGetProperty("@mt", out _)), Is.All.True);
        var joined = entries.Single(entry => EventName(entry) == "PlayerJoined" && Text(entry, "GameCode") == ann.Code);
        Assert.That(Text(joined, "PlayerName"), Is.EqualTo("Bob"));
        var shot = entries.Single(entry => EventName(entry) == "ShotFired" && Text(entry, "GameCode") == ann.Code);
        Assert.That(Text(shot, "PlayerName"), Is.EqualTo("Ann"));
        Assert.That(shot.GetProperty("Target").GetProperty("X").GetInt32(), Is.EqualTo(2));
        Assert.That(shot.GetProperty("Target").GetProperty("Y").GetInt32(), Is.EqualTo(4));
        Assert.That(Text(shot, "Result"), Is.AnyOf("Hit", "Miss"));
        var rejection = entries.Single(entry => EventName(entry) == "RequestRejected" && Text(entry, "RequestPath") == $"/api/games/{ann.Code}");
        Assert.That(rejection.GetProperty("StatusCode").GetInt32(), Is.EqualTo(401));
        Assert.That(Text(rejection, "Detail"), Is.Not.Empty);
    }

    [Test]
    public async Task PlayerTokens_NeverAppearInTheLogFile()
    {
        GameClient ann;
        GameClient bob;
        var factory = new FileLogApiFactory(LogPath);
        await using (factory)
        {
            var http = factory.CreateClient();
            ann = await GameClient.CreateRoomAsync(http, "Ann");
            bob = await GameClient.JoinAsync(http, ann.Code, "Bob");
            await using var annHub = await HubClient.ConnectAsync(factory, ann);
            await using var bobHub = await HubClient.ConnectAsync(factory, bob);
            await ann.PlaceFleetAsync();
            await bob.PlaceFleetAsync();
            await ann.FireOkAsync(0, 0);
            await bob.FireOkAsync(0, 0);
            await ann.GetAsync("wrong");
        }

        var (text, entries) = await ReadLogAsync();

        Assert.That(ann.Token, Is.Not.Empty);
        Assert.That(bob.Token, Is.Not.Empty);
        Assert.That(text, Does.Not.Contain(ann.Token));
        Assert.That(text, Does.Not.Contain(bob.Token));
        var propertyNames = entries.SelectMany(entry => entry.EnumerateObject()).Select(property => property.Name);
        Assert.That(propertyNames.Where(name => name.Contains("token", StringComparison.OrdinalIgnoreCase)), Is.Empty);
    }

    [Test]
    public async Task UnhandledException_Returns500AsProblemDetailsAndIsLoggedAsAnErrorWithTheException()
    {
        HttpResponseMessage response;
        var factory = new FileLogApiFactory(LogPath, failToCreateGames: true);
        await using (factory)
        {
            response = await GameClient.SendAsync(factory.CreateClient(), HttpMethod.Post, "/api/games", null, new { playerName = "Ann", opponent = "Random" });
        }

        await GameClient.AssertProblemAsync(response, 500);
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("boom"));
        var (_, entries) = await ReadLogAsync();
        var errors = entries.Where(entry => Text(entry, "@l") == "Error").ToList();
        Assert.That(errors.Select(entry => Text(entry, "@x")), Has.Some.Matches(Does.Contain("InvalidOperationException").And.Contain("boom")));
    }
}
