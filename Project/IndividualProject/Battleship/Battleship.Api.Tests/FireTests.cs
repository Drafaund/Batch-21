using Battleship.Core;

namespace Battleship.Api.Tests;

public class FireTests : ApiTestFixture<ApiFactory>
{
    private async Task<GameClient> StartedGameAsync(string opponent = "Random")
    {
        var game = await GameClient.CreateAsync(Http, "Ann", opponent);
        await game.PlaceFleetAsync();
        return game;
    }

    private static int CountEnemy(GameView view, EnemyCell kind) => view.EnemyBoard.Cells.Sum(row => row.Count(cell => cell == kind));

    private static int CountOwnShots(GameView view) => view.OwnBoard.Cells.Sum(row => row.Count(cell => cell is OwnCell.Hit or OwnCell.Miss));

    [Test]
    public async Task Fire_BeforeAnyShipIsPlaced_Returns409()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.FireAsync(0, 0);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task Fire_WhileStillPlacing_Returns409()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.FireAsync(0, 0);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task Fire_ValidShot_ResultIsHitOrMissAndTheEnemyBoardShowsIt()
    {
        var game = await StartedGameAsync();

        var shot = await game.FireOkAsync(3, 3);

        Assert.That(new[] { ShotResult.Hit, ShotResult.Miss }, Does.Contain(shot.Result));
        var expected = shot.Result == ShotResult.Hit ? EnemyCell.Hit : EnemyCell.Miss;
        Assert.That(shot.View.EnemyBoard.Cells[3][3], Is.EqualTo(expected));
        Assert.That(CountEnemy(shot.View, expected), Is.EqualTo(1));
        Assert.That(CountEnemy(shot.View, EnemyCell.Hit) + CountEnemy(shot.View, EnemyCell.Miss), Is.EqualTo(1));
    }

    [Test]
    public async Task Fire_ValidShot_BotRepliesWithExactlyOneShotAndTheTurnComesBack()
    {
        var game = await StartedGameAsync();

        var shot = await game.FireOkAsync(0, 0);

        Assert.That(shot.BotShots, Has.Exactly(1).Items);
        var reply = shot.BotShots.Single();
        Assert.That(new[] { ShotResult.Hit, ShotResult.Miss }, Does.Contain(reply.Result));
        Assert.That(shot.View.YourTurn, Is.True);
        Assert.That(CountOwnShots(shot.View), Is.EqualTo(1));
        var mark = shot.View.OwnBoard.Cells[reply.Target.Y][reply.Target.X];
        Assert.That(mark, Is.EqualTo(reply.Result == ShotResult.Hit ? OwnCell.Hit : OwnCell.Miss));
    }

    [Test]
    public async Task Fire_ValidShot_IncreasesTheVersionByOne()
    {
        var game = await StartedGameAsync();
        var before = await game.ViewAsync();

        var shot = await game.FireOkAsync(0, 0);

        Assert.That(shot.View.Version, Is.EqualTo(before.Version + 1));
    }

    [Test]
    public async Task Fire_TwoValidShots_EachGetsOneBotReplyWithoutRepeatingCells()
    {
        var game = await StartedGameAsync("HuntTarget");

        var first = await game.FireOkAsync(0, 0);
        var second = await game.FireOkAsync(1, 0);

        var replies = first.BotShots.Concat(second.BotShots).Select(s => s.Target).ToList();
        Assert.That(replies.Count, Is.EqualTo(2));
        Assert.That(replies.Distinct().Count(), Is.EqualTo(2));
        Assert.That(CountOwnShots(second.View), Is.EqualTo(2));
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(10, 0)]
    [TestCase(0, 10)]
    [TestCase(99, 99)]
    public async Task Fire_OutsideTheBoard_ReturnsInvalidWithoutABotReplyAndKeepsTheTurn(int x, int y)
    {
        var game = await StartedGameAsync();
        var before = await game.ViewAsync();

        var shot = await game.FireOkAsync(x, y);

        Assert.That(shot.Result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(shot.BotShots, Is.Empty);
        Assert.That(shot.View.YourTurn, Is.True);
        Assert.That(shot.View.Version, Is.EqualTo(before.Version));
        Assert.That(CountOwnShots(shot.View), Is.EqualTo(0));
        Assert.That(CountEnemy(shot.View, EnemyCell.Hit) + CountEnemy(shot.View, EnemyCell.Miss), Is.EqualTo(0));
    }

    [Test]
    public async Task Fire_SameCellTwice_SecondShotIsInvalidAndTheBotDoesNotFireAgain()
    {
        var game = await StartedGameAsync();
        await game.FireOkAsync(5, 5);
        var before = await game.ViewAsync();

        var second = await game.FireOkAsync(5, 5);

        Assert.That(second.Result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(second.BotShots, Is.Empty);
        Assert.That(second.View.YourTurn, Is.True);
        Assert.That(second.View.Version, Is.EqualTo(before.Version));
        Assert.That(CountOwnShots(second.View), Is.EqualTo(1));
    }

    [Test]
    public async Task Fire_MissingTarget_Returns400()
    {
        var game = await StartedGameAsync();

        var response = await game.PostRawAsync("shots", new { });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Fire_NoBody_Returns400()
    {
        var game = await StartedGameAsync();

        var response = await game.PostRawAsync("shots", null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Fire_NonNumericCoordinate_Returns400()
    {
        var game = await StartedGameAsync();

        var response = await game.PostRawAsync("shots", new { target = new { x = "a", y = "b" } });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Fire_NoToken_Returns401()
    {
        var game = await StartedGameAsync();

        var response = await game.FireAsync(null, 0, 0);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task Fire_WrongToken_Returns401AndNothingIsFired()
    {
        var game = await StartedGameAsync();

        var response = await game.FireAsync("wrong", 0, 0);

        await GameClient.AssertProblemAsync(response, 401);
        Assert.That(CountOwnShots(await game.ViewAsync()), Is.EqualTo(0));
    }

    [Test]
    public async Task Fire_UnknownCode_Returns404()
    {
        var response = await GameClient.SendAsync(Http, HttpMethod.Post, "/api/games/ZZZZZ/shots", "any", new { target = new { x = 0, y = 0 } });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Test]
    public async Task Fire_ShotLogs_CarryTheRequestIdAndPathOfTheRequestThatFiredThem()
    {
        var game = await StartedGameAsync();

        await game.FireOkAsync(2, 4);

        var shot = Factory.Logs.ForGame(game.Code).First(entry => entry.EventName == LogEvents.ShotFired);
        Assert.That(shot.Properties["RequestPath"], Is.EqualTo($"/api/games/{game.Code}/shots"));
        Assert.That(shot.Properties["RequestId"], Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Fire_ShotsAreWrittenToTheLog()
    {
        var game = await StartedGameAsync();

        await game.FireOkAsync(2, 4);

        var shots = Factory.Logs.ForGame(game.Code)
            .Where(entry => entry.EventName == LogEvents.ShotFired)
            .Select(entry => (Player: (string?)entry.Properties["PlayerName"], Target: (IReadOnlyDictionary<string, object?>)entry.Properties["Target"]!))
            .ToList();
        var annShot = shots.Single(shot => shot.Player == "Ann");
        Assert.That(annShot.Target["X"], Is.EqualTo(2));
        Assert.That(annShot.Target["Y"], Is.EqualTo(4));
        Assert.That(shots.Select(shot => shot.Player), Does.Contain("Bot Random"));
    }
}
