using Serilog.Events;

namespace Battleship.Api.Tests;

public class ErrorHandlingTests : ApiTestFixture<ApiFactory>
{
    [Test]
    public async Task ExpectedErrors_AreLoggedAsInformationAndNeverAsErrors()
    {
        var game = await GameClient.CreateAsync(Http);

        await game.GetAsync(token: null);
        await game.FireAsync(0, 0);
        await GameClient.SendAsync(Http, HttpMethod.Get, "/api/games/ZZZZZ", "x", null);
        await GameClient.SendAsync(Http, HttpMethod.Post, "/api/games", null, new { });

        var rejected = Factory.Logs.Entries.Where(e => e.EventName == LogEvents.RequestRejected).ToList();
        Assert.That(rejected.Count >= 4, Is.True);
        Assert.That(rejected.Select(e => e.Level), Is.All.EqualTo(LogEventLevel.Information));
        Assert.That(Factory.Logs.Entries.Any(e => e.Level >= LogEventLevel.Error), Is.False);
    }

    [Test]
    public async Task RejectionLog_NamesTheStatusAndThePath()
    {
        var game = await GameClient.CreateAsync(Http);

        await game.GetAsync("wrong");

        var rejection = Factory.Logs.Entries.Single(entry =>
            entry.EventName == LogEvents.RequestRejected && Equals(entry.Properties.GetValueOrDefault("RequestPath"), $"/api/games/{game.Code}"));
        Assert.That(rejection.Properties["StatusCode"], Is.EqualTo(401));
    }

    [Test]
    public async Task WrongHttpMethod_Returns405()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await GameClient.SendAsync(Http, HttpMethod.Get, $"/api/games/{game.Code}/shots", game.Token, null);

        await GameClient.AssertProblemAsync(response, 405);
    }

    [Test]
    public async Task UnknownRoute_Returns404AsProblemDetails()
    {
        var response = await Http.GetAsync("/api/nothing");

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Test]
    public async Task WrongContentType_Returns415AsProblemDetails()
    {
        var content = new StringContent("playerName=Ann", System.Text.Encoding.UTF8, "text/plain");

        var response = await Http.PostAsync("/api/games", content);

        await GameClient.AssertProblemAsync(response, 415);
    }
}
