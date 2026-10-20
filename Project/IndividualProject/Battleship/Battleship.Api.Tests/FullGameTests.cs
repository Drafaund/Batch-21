using Battleship.Core;

namespace Battleship.Api.Tests;

public class FullGameTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _http;

    public FullGameTests(ApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    /// <summary>Fires at the cells row by row until the game ends, checking every response on the way.</summary>
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
                Assert.NotEqual(ShotResult.Invalid, last.Result);
                botShots.AddRange(last.BotShots);
            }
        }

        return (last!, shots, botShots);
    }

    [Theory]
    [InlineData("Random")]
    [InlineData("HuntTarget")]
    public async Task FullGame_PlayedOverHttp_EndsWithAWinnerAndAConsistentFinalView(string opponent)
    {
        var game = await GameClient.CreateAsync(_http, "Ann", opponent);
        await game.PlaceRandomAsync();

        var (last, shots, botShots) = await PlayToTheEndAsync(game);

        var view = last.View;
        Assert.Equal(GamePhase.Finished, view.Phase);
        Assert.False(view.YourTurn);
        Assert.Contains(view.Winner, new[] { "you", "opponent" });
        Assert.InRange(shots, 17, 100);

        if (view.Winner == "you")
        {
            Assert.Equal(0, view.EnemyShipsRemaining);
            Assert.Equal(17, view.EnemyBoard.Cells.Sum(row => row.Count(c => c == EnemyCell.Hit)));
            Assert.Contains(view.OwnShips, ship => !ship.IsSunk);
        }
        else
        {
            Assert.All(view.OwnShips, ship => Assert.True(ship.IsSunk));
            Assert.True(view.EnemyShipsRemaining > 0);
        }

        Assert.Equal(botShots.Count, botShots.Select(s => s.Target).Distinct().Count());
        Assert.All(botShots, s => Assert.NotEqual(ShotResult.Invalid, s.Result));
    }

    [Fact]
    public async Task FullGame_AfterItEnded_ShotsAndShipsAreRefusedButTheViewStaysReadable()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceRandomAsync();
        await PlayToTheEndAsync(game);

        await GameClient.AssertProblemAsync(await game.FireAsync(0, 0), 409);
        await GameClient.AssertProblemAsync(await game.PlaceShipAsync(ShipType.Destroyer, 0, 0, Orientation.Horizontal), 409);
        Assert.Equal(GamePhase.Finished, (await game.ViewAsync()).Phase);
    }

    [Fact]
    public async Task FullGame_EndsOnceAndTheWinnerIsLogged()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceRandomAsync();

        await PlayToTheEndAsync(game);

        var wins = _factory.Logs.Messages.Count(m => m.Contains($"Game {game.Code}") && m.EndsWith("won"));
        Assert.Equal(1, wins);
    }

    [Fact]
    public async Task FullGame_WinningShot_ReportsSunkOrIsFollowedByNoBotReply()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceRandomAsync();

        var (last, _, _) = await PlayToTheEndAsync(game);

        if (last.View.Winner == "you")
        {
            Assert.Equal(ShotResult.Sunk, last.Result);
            Assert.Empty(last.BotShots);
        }
        else
        {
            Assert.Single(last.BotShots);
            Assert.Equal(ShotResult.Sunk, last.BotShots[0].Result);
        }
    }

    [Fact]
    public async Task FullGame_ManualPlacementThenAllShots_AlsoFinishes()
    {
        var game = await GameClient.CreateAsync(_http, "Ann", "HuntTarget");
        await game.PlaceFleetAsync();

        var (last, _, _) = await PlayToTheEndAsync(game);

        Assert.Equal(GamePhase.Finished, last.View.Phase);
    }
}
