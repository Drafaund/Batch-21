using System.Net;
using System.Net.Http.Json;

namespace Battleship.Api.Tests;

public class CreateGameTests : IClassFixture<ApiFactory>
{
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly HttpClient _http;

    public CreateGameTests(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    private Task<HttpResponseMessage> PostAsync(object? body) => GameClient.SendAsync(_http, HttpMethod.Post, "/api/games", null, body);

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationCodeTokenAndSeat()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "Random" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await GameClient.ReadAsync<CreateGameResponse>(response);
        Assert.Equal($"/api/games/{created.Code}", response.Headers.Location?.OriginalString);
        Assert.Equal(1, created.Seat);
        Assert.False(string.IsNullOrWhiteSpace(created.PlayerToken));
        Assert.Equal(created.Code, created.View.Code);
    }

    [Fact]
    public async Task Create_ManyGames_CodesHaveFiveReadableCharactersAndAreUnique()
    {
        var codes = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            codes.Add((await GameClient.CreateAsync(_http)).Code);
        }

        Assert.All(codes, code =>
        {
            Assert.Equal(5, code.Length);
            Assert.All(code, c => Assert.Contains(c, CodeAlphabet));
        });
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public async Task Create_ManyGames_TokensAreUnique()
    {
        var tokens = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            tokens.Add((await GameClient.CreateAsync(_http)).Token);
        }

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    [Fact]
    public async Task Create_NewGame_ViewDescribesEmptyBoardsInPlacingPhase()
    {
        var game = await GameClient.CreateAsync(_http, "Ann", "Random");

        var view = game.InitialView;
        Assert.Equal(GamePhase.Placing, view.Phase);
        Assert.Equal(1, view.Version);
        Assert.Equal(new PlayerView(1, "Ann"), view.You);
        Assert.Equal(new OpponentView("Bot Random", true), view.Opponent);
        Assert.False(view.YourTurn);
        Assert.Null(view.Winner);
        Assert.Empty(view.Placement.Placed);
        Assert.Equal(5, view.Placement.Remaining.Count);
        Assert.Empty(view.OwnShips);
        Assert.Equal(10, view.OwnBoard.Size);
        Assert.Equal(10, view.OwnBoard.Cells.Length);
        Assert.All(view.OwnBoard.Cells, row => Assert.All(row, cell => Assert.Equal(OwnCell.Water, cell)));
        Assert.Equal(10, view.EnemyBoard.Size);
        Assert.All(view.EnemyBoard.Cells, row => Assert.All(row, cell => Assert.Equal(EnemyCell.Unknown, cell)));
        Assert.Equal(5, view.EnemyShipsRemaining);
    }

    [Fact]
    public async Task Create_HuntTargetOpponent_BotIsNamedBotHunter()
    {
        var game = await GameClient.CreateAsync(_http, "Ann", "HuntTarget");

        Assert.Equal("Bot Hunter", game.InitialView.Opponent?.Name);
    }

    [Fact]
    public async Task Create_LowerCaseOpponent_IsAccepted()
    {
        var game = await GameClient.CreateAsync(_http, "Ann", "huntTarget");

        Assert.Equal("Bot Hunter", game.InitialView.Opponent?.Name);
    }

    [Fact]
    public async Task Create_NameWithSpaces_IsTrimmed()
    {
        var game = await GameClient.CreateAsync(_http, "  Ann  ");

        Assert.Equal("Ann", game.InitialView.You.Name);
    }

    [Fact]
    public async Task Create_NameWithMarkup_IsKeptAsPlainText()
    {
        var game = await GameClient.CreateAsync(_http, "<b>Ann</b>");

        Assert.Equal("<b>Ann</b>", game.InitialView.You.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901")]
    [InlineData("An\nn")]
    [InlineData("An\tn")]
    public async Task Create_InvalidName_Returns400(string name)
    {
        var response = await PostAsync(new { playerName = name, opponent = "Random" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_NameOfTwentyCharacters_IsAccepted()
    {
        var game = await GameClient.CreateAsync(_http, new string('a', 20));

        Assert.Equal(20, game.InitialView.You.Name.Length);
    }

    [Fact]
    public async Task Create_MissingName_Returns400()
    {
        var response = await PostAsync(new { opponent = "Random" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_MissingOpponent_Returns400()
    {
        var response = await PostAsync(new { playerName = "Ann" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_UnknownOpponent_Returns400()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "robot" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_NumericOpponent_Returns400()
    {
        var response = await _http.PostAsJsonAsync("/api/games", new { playerName = "Ann", opponent = 0 });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_HumanOpponent_Returns201AndWaitsInTheLobby()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "Human" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await GameClient.ReadAsync<CreateGameResponse>(response);
        Assert.Equal(1, created.Seat);
        Assert.Equal(GamePhase.Lobby, created.View.Phase);
        Assert.Null(created.View.Opponent);
        Assert.False(created.View.YourTurn);
    }

    [Fact]
    public async Task Create_EmptyBody_Returns400()
    {
        var response = await PostAsync(null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400()
    {
        var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("/api/games", content);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Create_Response_WritesEnumsAsStringsAndPropertiesInCamelCase()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "Random" });

        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"phase\":\"Placing\"", text);
        Assert.Contains("\"playerToken\":", text);
        Assert.Contains("\"enemyShipsRemaining\":5", text);
    }
}
