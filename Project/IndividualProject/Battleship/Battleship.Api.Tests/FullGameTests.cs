using Battleship.Core;

namespace Battleship.Api.Tests;

public class FullGameTests : ApiTestFixture<ApiFactory>
{
    private static async Task<(ShotResponse Last, int Shots, List<BotShotView> BotShots)> PlayToTheEndAsync(GameClient game)
    {
        ShotResponse? last = null;
        var shots = 0;
        var botShots = new List<BotShotView>();

        for (var y = 0; y < 10 && last?.View.Phase != GamePhase.Finished; y++)
        {
            for (var x = 0; x < 10 && last?.View.Phase != GamePhase.Finished; x++)
            {
                last = await game.FireOkAsync(x, y);
                shots++;
                Assert.That(last.Result, Is.Not.EqualTo(ShotResult.Invalid));
                botShots.AddRange(last.BotShots);
            }
        }

        return (last!, shots, botShots);
    }

    [TestCase("Random")]
    [TestCase("HuntTarget")]
    public async Task FullGame_PlayedOverHttp_EndsWithAWinnerAndAConsistentFinalView(string opponent)
    {
        var game = await GameClient.CreateAsync(Http, "Ann", opponent);
        await game.PlaceRandomAsync();

        var (last, shots, botShots) = await PlayToTheEndAsync(game);

        var view = last.View;
        Assert.That(view.Phase, Is.EqualTo(GamePhase.Finished));
        Assert.That(view.YourTurn, Is.False);
        Assert.That(new[] { "you", "opponent" }, Does.Contain(view.Winner));
        Assert.That(shots, Is.InRange(17, 100));

        if (view.Winner == "you")
        {
            Assert.That(view.EnemyShipsRemaining, Is.EqualTo(0));
            Assert.That(view.EnemyBoard.Cells.Sum(row => row.Count(c => c == EnemyCell.Sunk)), Is.EqualTo(17));
            Assert.That(view.EnemyBoard.Cells.Sum(row => row.Count(c => c == EnemyCell.Hit)), Is.EqualTo(0));
            Assert.That(view.OwnShips.Any(ship => !ship.IsSunk), Is.True);
        }
        else
        {
            Assert.That(view.OwnShips.Select(ship => ship.IsSunk), Is.All.True);
            Assert.That(view.EnemyShipsRemaining > 0, Is.True);
        }

        Assert.That(botShots.Select(s => s.Target).Distinct().Count(), Is.EqualTo(botShots.Count));
        Assert.That(botShots.Select(s => s.Result), Is.All.Not.EqualTo(ShotResult.Invalid));
    }

    [Test]
    public async Task FullGame_AfterItEnded_ShotsAndShipsAreRefusedButTheViewStaysReadable()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceRandomAsync();
        await PlayToTheEndAsync(game);

        await GameClient.AssertProblemAsync(await game.FireAsync(0, 0), 409);
        await GameClient.AssertProblemAsync(await game.PlaceShipAsync(ShipType.Destroyer, 0, 0, Orientation.Horizontal), 409);
        Assert.That((await game.ViewAsync()).Phase, Is.EqualTo(GamePhase.Finished));
    }

    [Test]
    public async Task FullGame_EndsOnceAndTheWinnerIsLogged()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceRandomAsync();

        await PlayToTheEndAsync(game);

        var wins = Factory.Logs.ForGame(game.Code).Count(entry => entry.EventName == LogEvents.GameWon);
        Assert.That(wins, Is.EqualTo(1));
    }

    [Test]
    public async Task FullGame_WinningShot_ReportsSunkOrIsFollowedByNoBotReply()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceRandomAsync();

        var (last, _, _) = await PlayToTheEndAsync(game);

        if (last.View.Winner == "you")
        {
            Assert.That(last.Result, Is.EqualTo(ShotResult.Sunk));
            Assert.That(last.BotShots, Is.Empty);
        }
        else
        {
            Assert.That(last.BotShots, Has.Exactly(1).Items);
            Assert.That(last.BotShots[0].Result, Is.EqualTo(ShotResult.Sunk));
        }
    }

    [Test]
    public async Task FullGame_ManualPlacementThenAllShots_AlsoFinishes()
    {
        var game = await GameClient.CreateAsync(Http, "Ann", "HuntTarget");
        await game.PlaceFleetAsync();

        var (last, _, _) = await PlayToTheEndAsync(game);

        Assert.That(last.View.Phase, Is.EqualTo(GamePhase.Finished));
    }
}
