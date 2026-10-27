using Microsoft.Extensions.Logging;

namespace Battleship.Api.Tests;

public class ErrorHandlingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _http;

    public ErrorHandlingTests(ApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ExpectedErrors_AreLoggedAsInformationAndNeverAsErrors()
    {
        var game = await GameClient.CreateAsync(_http);

        await game.GetAsync(token: null);
        await game.FireAsync(0, 0);
        await GameClient.SendAsync(_http, HttpMethod.Get, "/api/games/ZZZZZ", "x", null);
        await GameClient.SendAsync(_http, HttpMethod.Post, "/api/games", null, new { });

        var rejected = _factory.Logs.Entries.Where(e => e.Message.Contains("rejected with")).ToList();
        Assert.True(rejected.Count >= 4);
        Assert.All(rejected, e => Assert.Equal(LogLevel.Information, e.Level));
        Assert.DoesNotContain(_factory.Logs.Entries, e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task RejectionLog_NamesTheStatusAndThePath()
    {
        var game = await GameClient.CreateAsync(_http);

        await game.GetAsync("wrong");

        Assert.Contains(_factory.Logs.Messages, m => m.Contains($"/api/games/{game.Code}") && m.Contains("rejected with 401"));
    }

    [Fact]
    public async Task WrongHttpMethod_Returns405()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await GameClient.SendAsync(_http, HttpMethod.Get, $"/api/games/{game.Code}/shots", game.Token, null);

        await GameClient.AssertProblemAsync(response, 405);
    }

    [Fact]
    public async Task UnknownRoute_Returns404AsProblemDetails()
    {
        var response = await _http.GetAsync("/api/nothing");

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Fact]
    public async Task WrongContentType_Returns415AsProblemDetails()
    {
        var content = new StringContent("playerName=Ann", System.Text.Encoding.UTF8, "text/plain");

        var response = await _http.PostAsync("/api/games", content);

        await GameClient.AssertProblemAsync(response, 415);
    }
}
