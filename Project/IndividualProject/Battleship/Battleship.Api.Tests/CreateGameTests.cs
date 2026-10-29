using System.Net;
using System.Net.Http.Json;

namespace Battleship.Api.Tests;

public class CreateGameTests : ApiTestFixture<ApiFactory>
{
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private Task<HttpResponseMessage> PostAsync(object? body) => GameClient.SendAsync(Http, HttpMethod.Post, "/api/games", null, body);

    [Test]
    public async Task Create_ValidRequest_Returns201WithLocationCodeTokenAndSeat()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "Random" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await GameClient.ReadAsync<CreateGameResponse>(response);
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo($"/api/games/{created.Code}"));
        Assert.That(created.Seat, Is.EqualTo(1));
        Assert.That(string.IsNullOrWhiteSpace(created.PlayerToken), Is.False);
        Assert.That(created.View.Code, Is.EqualTo(created.Code));
    }

    [Test]
    public async Task Create_ManyGames_CodesHaveFiveReadableCharactersAndAreUnique()
    {
        var codes = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            codes.Add((await GameClient.CreateAsync(Http)).Code);
        }

        Assert.That(codes.Select(code => code.Length), Is.All.EqualTo(5));
        Assert.That(codes.SelectMany(code => code), Has.All.Matches<char>(c => CodeAlphabet.Contains(c)));
        Assert.That(codes.Distinct().Count(), Is.EqualTo(codes.Count));
    }

    [Test]
    public async Task Create_ManyGames_TokensAreUnique()
    {
        var tokens = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            tokens.Add((await GameClient.CreateAsync(Http)).Token);
        }

        Assert.That(tokens.Distinct().Count(), Is.EqualTo(tokens.Count));
    }

    [Test]
    public async Task Create_NewGame_ViewDescribesEmptyBoardsInPlacingPhase()
    {
        var game = await GameClient.CreateAsync(Http, "Ann", "Random");

        var view = game.InitialView;
        Assert.That(view.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(view.Version, Is.EqualTo(1));
        Assert.That(view.You, Is.EqualTo(new PlayerView(1, "Ann")));
        Assert.That(view.Opponent, Is.EqualTo(new OpponentView("Bot Random", true)));
        Assert.That(view.YourTurn, Is.False);
        Assert.That(view.Winner, Is.Null);
        Assert.That(view.Placement.Placed, Is.Empty);
        Assert.That(view.Placement.Remaining.Count, Is.EqualTo(5));
        Assert.That(view.OwnShips, Is.Empty);
        Assert.That(view.OwnBoard.Size, Is.EqualTo(10));
        Assert.That(view.OwnBoard.Cells.Length, Is.EqualTo(10));
        Assert.That(view.OwnBoard.Cells.SelectMany(row => row), Is.All.EqualTo(OwnCell.Water));
        Assert.That(view.EnemyBoard.Size, Is.EqualTo(10));
        Assert.That(view.EnemyBoard.Cells.SelectMany(row => row), Is.All.EqualTo(EnemyCell.Unknown));
        Assert.That(view.EnemyShipsRemaining, Is.EqualTo(5));
    }

    [Test]
    public async Task Create_HuntTargetOpponent_BotIsNamedBotHunter()
    {
        var game = await GameClient.CreateAsync(Http, "Ann", "HuntTarget");

        Assert.That(game.InitialView.Opponent?.Name, Is.EqualTo("Bot Hunter"));
    }

    [Test]
    public async Task Create_LowerCaseOpponent_IsAccepted()
    {
        var game = await GameClient.CreateAsync(Http, "Ann", "huntTarget");

        Assert.That(game.InitialView.Opponent?.Name, Is.EqualTo("Bot Hunter"));
    }

    [Test]
    public async Task Create_NameWithSpaces_IsTrimmed()
    {
        var game = await GameClient.CreateAsync(Http, "  Ann  ");

        Assert.That(game.InitialView.You.Name, Is.EqualTo("Ann"));
    }

    [Test]
    public async Task Create_NameWithMarkup_IsKeptAsPlainText()
    {
        var game = await GameClient.CreateAsync(Http, "<b>Ann</b>");

        Assert.That(game.InitialView.You.Name, Is.EqualTo("<b>Ann</b>"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("123456789012345678901")]
    [TestCase("An\nn")]
    [TestCase("An\tn")]
    public async Task Create_InvalidName_Returns400(string name)
    {
        var response = await PostAsync(new { playerName = name, opponent = "Random" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_NameOfTwentyCharacters_IsAccepted()
    {
        var game = await GameClient.CreateAsync(Http, new string('a', 20));

        Assert.That(game.InitialView.You.Name.Length, Is.EqualTo(20));
    }

    [Test]
    public async Task Create_MissingName_Returns400()
    {
        var response = await PostAsync(new { opponent = "Random" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_MissingOpponent_Returns400()
    {
        var response = await PostAsync(new { playerName = "Ann" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_UnknownOpponent_Returns400()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "robot" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_NumericOpponent_Returns400()
    {
        var response = await Http.PostAsJsonAsync("/api/games", new { playerName = "Ann", opponent = 0 });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_HumanOpponent_Returns201AndWaitsInTheLobby()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "Human" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await GameClient.ReadAsync<CreateGameResponse>(response);
        Assert.That(created.Seat, Is.EqualTo(1));
        Assert.That(created.View.Phase, Is.EqualTo(GamePhase.Lobby));
        Assert.That(created.View.Opponent, Is.Null);
        Assert.That(created.View.YourTurn, Is.False);
    }

    [Test]
    public async Task Create_EmptyBody_Returns400()
    {
        var response = await PostAsync(null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_MalformedJson_Returns400()
    {
        var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await Http.PostAsync("/api/games", content);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Create_Response_WritesEnumsAsStringsAndPropertiesInCamelCase()
    {
        var response = await PostAsync(new { playerName = "Ann", opponent = "Random" });

        var text = await response.Content.ReadAsStringAsync();
        Assert.That(text, Does.Contain("\"phase\":\"Placing\""));
        Assert.That(text, Does.Contain("\"playerToken\":"));
        Assert.That(text, Does.Contain("\"enemyShipsRemaining\":5"));
    }
}
