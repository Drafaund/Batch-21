namespace Battleship.Api.Tests;

public class GetGameTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http;

    public GetGameTests(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WithToken_ReturnsTheSameViewAsCreate()
    {
        var game = await GameClient.CreateAsync(_http);

        var view = await game.ViewAsync();

        Assert.Equal(game.InitialView, view, new GameViewComparer());
    }

    [Fact]
    public async Task Get_LowerCaseCode_FindsTheGame()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await GameClient.SendAsync(_http, HttpMethod.Get, $"/api/games/{game.Code.ToLowerInvariant()}", game.Token, null);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Get_NoToken_Returns401()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.GetAsync(token: null);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
    public async Task Get_WrongToken_Returns401()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.GetAsync("not-the-token");

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
    public async Task Get_TokenOfAnotherGame_Returns401()
    {
        var first = await GameClient.CreateAsync(_http);
        var second = await GameClient.CreateAsync(_http);

        var response = await first.GetAsync(second.Token);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
    public async Task Get_UnknownCode_Returns404()
    {
        var response = await GameClient.SendAsync(_http, HttpMethod.Get, "/api/games/ZZZZZ", "any", null);

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Fact]
    public async Task Get_Response_NeverContainsTheToken()
    {
        var game = await GameClient.CreateAsync(_http);

        var text = await (await game.GetAsync()).Content.ReadAsStringAsync();

        Assert.DoesNotContain(game.Token, text);
    }

    [Fact]
    public async Task Get_TwoGames_AreIndependent()
    {
        var first = await GameClient.CreateAsync(_http, "Ann");
        var second = await GameClient.CreateAsync(_http, "Bob");

        await first.PlaceFleetAsync();

        Assert.Equal(GamePhase.InProgress, (await first.ViewAsync()).Phase);
        Assert.Equal(GamePhase.Placing, (await second.ViewAsync()).Phase);
        Assert.Equal("Bob", (await second.ViewAsync()).You.Name);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await _http.GetAsync("/api/nothing");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class GameViewComparer : IEqualityComparer<GameView>
    {
        public bool Equals(GameView? x, GameView? y) => Serialize(x) == Serialize(y);

        public int GetHashCode(GameView obj) => Serialize(obj).GetHashCode();

        private static string Serialize(GameView? view) => System.Text.Json.JsonSerializer.Serialize(view, GameClient.Json);
    }
}
