namespace Battleship.Api.Tests;

public class GetGameTests : ApiTestFixture<ApiFactory>
{
    [Test]
    public async Task Get_WithToken_ReturnsTheSameViewAsCreate()
    {
        var game = await GameClient.CreateAsync(Http);

        var view = await game.ViewAsync();

        Assert.That(view, Is.EqualTo(game.InitialView).Using(new GameViewComparer()));
    }

    [Test]
    public async Task Get_LowerCaseCode_FindsTheGame()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await GameClient.SendAsync(Http, HttpMethod.Get, $"/api/games/{game.Code.ToLowerInvariant()}", game.Token, null);

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [Test]
    public async Task Get_NoToken_Returns401()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.GetAsync(token: null);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task Get_WrongToken_Returns401()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.GetAsync("not-the-token");

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task Get_TokenOfAnotherGame_Returns401()
    {
        var first = await GameClient.CreateAsync(Http);
        var second = await GameClient.CreateAsync(Http);

        var response = await first.GetAsync(second.Token);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task Get_UnknownCode_Returns404()
    {
        var response = await GameClient.SendAsync(Http, HttpMethod.Get, "/api/games/ZZZZZ", "any", null);

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Test]
    public async Task Get_Response_NeverContainsTheToken()
    {
        var game = await GameClient.CreateAsync(Http);

        var text = await (await game.GetAsync()).Content.ReadAsStringAsync();

        Assert.That(text, Does.Not.Contain(game.Token));
    }

    [Test]
    public async Task Get_TwoGames_AreIndependent()
    {
        var first = await GameClient.CreateAsync(Http, "Ann");
        var second = await GameClient.CreateAsync(Http, "Bob");

        await first.PlaceFleetAsync();

        Assert.That((await first.ViewAsync()).Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That((await second.ViewAsync()).Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That((await second.ViewAsync()).You.Name, Is.EqualTo("Bob"));
    }

    [Test]
    public async Task UnknownRoute_Returns404()
    {
        var response = await Http.GetAsync("/api/nothing");

        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    private sealed class GameViewComparer : IEqualityComparer<GameView>
    {
        public bool Equals(GameView? x, GameView? y) => Serialize(x) == Serialize(y);

        public int GetHashCode(GameView obj) => Serialize(obj).GetHashCode();

        private static string Serialize(GameView? view) => System.Text.Json.JsonSerializer.Serialize(view, GameClient.Json);
    }
}
