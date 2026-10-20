using Battleship.Core;

namespace Battleship.Api.Tests;

public class FireTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _http;

    public FireTests(ApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    private async Task<GameClient> StartedGameAsync(string opponent = "Random")
    {
        var game = await GameClient.CreateAsync(_http, "Ann", opponent);
        await game.PlaceFleetAsync();
        return game;
    }

    private static int CountEnemy(GameView view, EnemyCell kind) => view.EnemyBoard.Cells.Sum(row => row.Count(cell => cell == kind));

    private static int CountOwnShots(GameView view) => view.OwnBoard.Cells.Sum(row => row.Count(cell => cell is OwnCell.Hit or OwnCell.Miss));

    [Fact]
    public async Task Fire_BeforeAnyShipIsPlaced_Returns409()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.FireAsync(0, 0);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task Fire_WhileStillPlacing_Returns409()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.FireAsync(0, 0);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task Fire_ValidShot_ResultIsHitOrMissAndTheEnemyBoardShowsIt()
    {
        var game = await StartedGameAsync();

        var shot = await game.FireOkAsync(3, 3);

        Assert.Contains(shot.Result, new[] { ShotResult.Hit, ShotResult.Miss });
        var expected = shot.Result == ShotResult.Hit ? EnemyCell.Hit : EnemyCell.Miss;
        Assert.Equal(expected, shot.View.EnemyBoard.Cells[3][3]);
        Assert.Equal(1, CountEnemy(shot.View, expected));
        Assert.Equal(1, CountEnemy(shot.View, EnemyCell.Hit) + CountEnemy(shot.View, EnemyCell.Miss));
    }

    [Fact]
    public async Task Fire_ValidShot_BotRepliesWithExactlyOneShotAndTheTurnComesBack()
    {
        var game = await StartedGameAsync();

        var shot = await game.FireOkAsync(0, 0);

        var reply = Assert.Single(shot.BotShots);
        Assert.Contains(reply.Result, new[] { ShotResult.Hit, ShotResult.Miss });
        Assert.True(shot.View.YourTurn);
        Assert.Equal(1, CountOwnShots(shot.View));
        var mark = shot.View.OwnBoard.Cells[reply.Target.Y][reply.Target.X];
        Assert.Equal(reply.Result == ShotResult.Hit ? OwnCell.Hit : OwnCell.Miss, mark);
    }

    [Fact]
    public async Task Fire_ValidShot_IncreasesTheVersionByOne()
    {
        var game = await StartedGameAsync();
        var before = await game.ViewAsync();

        var shot = await game.FireOkAsync(0, 0);

        Assert.Equal(before.Version + 1, shot.View.Version);
    }

    [Fact]
    public async Task Fire_TwoValidShots_EachGetsOneBotReplyWithoutRepeatingCells()
    {
        var game = await StartedGameAsync("HuntTarget");

        var first = await game.FireOkAsync(0, 0);
        var second = await game.FireOkAsync(1, 0);

        var replies = first.BotShots.Concat(second.BotShots).Select(s => s.Target).ToList();
        Assert.Equal(2, replies.Count);
        Assert.Equal(2, replies.Distinct().Count());
        Assert.Equal(2, CountOwnShots(second.View));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(10, 0)]
    [InlineData(0, 10)]
    [InlineData(99, 99)]
    public async Task Fire_OutsideTheBoard_ReturnsInvalidWithoutABotReplyAndKeepsTheTurn(int x, int y)
    {
        var game = await StartedGameAsync();
        var before = await game.ViewAsync();

        var shot = await game.FireOkAsync(x, y);

        Assert.Equal(ShotResult.Invalid, shot.Result);
        Assert.Empty(shot.BotShots);
        Assert.True(shot.View.YourTurn);
        Assert.Equal(before.Version, shot.View.Version);
        Assert.Equal(0, CountOwnShots(shot.View));
        Assert.Equal(0, CountEnemy(shot.View, EnemyCell.Hit) + CountEnemy(shot.View, EnemyCell.Miss));
    }

    [Fact]
    public async Task Fire_SameCellTwice_SecondShotIsInvalidAndTheBotDoesNotFireAgain()
    {
        var game = await StartedGameAsync();
        await game.FireOkAsync(5, 5);
        var before = await game.ViewAsync();

        var second = await game.FireOkAsync(5, 5);

        Assert.Equal(ShotResult.Invalid, second.Result);
        Assert.Empty(second.BotShots);
        Assert.True(second.View.YourTurn);
        Assert.Equal(before.Version, second.View.Version);
        Assert.Equal(1, CountOwnShots(second.View));
    }

    [Fact]
    public async Task Fire_MissingTarget_Returns400()
    {
        var game = await StartedGameAsync();

        var response = await game.PostRawAsync("shots", new { });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Fire_NoBody_Returns400()
    {
        var game = await StartedGameAsync();

        var response = await game.PostRawAsync("shots", null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Fire_NonNumericCoordinate_Returns400()
    {
        var game = await StartedGameAsync();

        var response = await game.PostRawAsync("shots", new { target = new { x = "a", y = "b" } });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Fire_NoToken_Returns401()
    {
        var game = await StartedGameAsync();

        var response = await game.FireAsync(null, 0, 0);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
    public async Task Fire_WrongToken_Returns401AndNothingIsFired()
    {
        var game = await StartedGameAsync();

        var response = await game.FireAsync("wrong", 0, 0);

        await GameClient.AssertProblemAsync(response, 401);
        Assert.Equal(0, CountOwnShots(await game.ViewAsync()));
    }

    [Fact]
    public async Task Fire_UnknownCode_Returns404()
    {
        var response = await GameClient.SendAsync(_http, HttpMethod.Post, "/api/games/ZZZZZ/shots", "any", new { target = new { x = 0, y = 0 } });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Fact]
    public async Task Fire_ShotsAreWrittenToTheLog()
    {
        var game = await StartedGameAsync();

        await game.FireOkAsync(2, 4);

        var messages = _factory.Logs.Messages;
        Assert.Contains(messages, m => m.Contains($"Game {game.Code}") && m.Contains("Ann fired at (2, 4)"));
        Assert.Contains(messages, m => m.Contains($"Game {game.Code}") && m.Contains("Bot Random fired at"));
    }
}
