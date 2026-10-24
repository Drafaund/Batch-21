using System.Text.Json;
using Battleship.Core;

namespace Battleship.Api.Tests;

/// <summary>
/// The enemy board marks the cells of a ship that is already sunk, and only those. Both fleets stand where
/// <see cref="GameClient.PlaceFleetAsync"/> puts them: Carrier row 0, Battleship row 1, Cruiser row 2,
/// Submarine row 3, Destroyer row 4, all starting at column 0. The Destroyer covers (0,4) and (1,4).
/// </summary>
public class SunkCellTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http;

    public SunkCellTests(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    private async Task<(GameClient Ann, GameClient Bob)> NewBattleAsync()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");
        var bob = await GameClient.JoinAsync(_http, ann.Code, "Bob");
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        return (ann, bob);
    }

    /// <summary>Ann fires at each target in turn; Bob answers every shot with a miss far from the fleet.</summary>
    private static async Task<ShotResponse> AnnFiresAsync(GameClient ann, GameClient bob, params (int X, int Y)[] targets)
    {
        ShotResponse? last = null;
        var bobTargets = new Queue<(int X, int Y)>(Enumerable.Range(0, 10).Select(x => (x, 9)));
        foreach (var (x, y) in targets)
        {
            last = await ann.FireOkAsync(x, y);
            var reply = bobTargets.Dequeue();
            await bob.FireOkAsync(reply.X, reply.Y);
        }

        return last!;
    }

    [Fact]
    public async Task EnemyBoard_ShipHitButNotSunk_ShowsHit()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await AnnFiresAsync(ann, bob, (0, 4));

        Assert.Equal(ShotResult.Hit, shot.Result);
        Assert.Equal(EnemyCell.Hit, shot.View.EnemyBoard.Cells[4][0]);
        Assert.Equal(0, CountEnemy(shot.View, EnemyCell.Sunk));
    }

    [Fact]
    public async Task EnemyBoard_ShipThatSinks_ShowsEveryCellOfThatShipAsSunk()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        Assert.Equal(ShotResult.Sunk, shot.Result);
        Assert.Equal(EnemyCell.Sunk, shot.View.EnemyBoard.Cells[4][0]);
        Assert.Equal(EnemyCell.Sunk, shot.View.EnemyBoard.Cells[4][1]);
        Assert.Equal(2, CountEnemy(shot.View, EnemyCell.Sunk));
        Assert.Equal(0, CountEnemy(shot.View, EnemyCell.Hit));
    }

    [Fact]
    public async Task EnemyBoard_ShipThatSinks_LeavesOtherShipsHitAndEverythingElseUnknown()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await AnnFiresAsync(ann, bob, (0, 0), (0, 4), (1, 4));

        Assert.Equal(EnemyCell.Hit, shot.View.EnemyBoard.Cells[0][0]);
        Assert.Equal(1, CountEnemy(shot.View, EnemyCell.Hit));
        Assert.Equal(2, CountEnemy(shot.View, EnemyCell.Sunk));
        Assert.Equal(97, CountEnemy(shot.View, EnemyCell.Unknown));
        Assert.Equal(EnemyCell.Unknown, shot.View.EnemyBoard.Cells[4][2]);
    }

    [Fact]
    public async Task EnemyBoard_AfterTheSinking_StaysSunkWhenTheViewIsLoadedAgain()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        var view = await ann.ViewAsync();

        Assert.Equal(EnemyCell.Sunk, view.EnemyBoard.Cells[4][0]);
        Assert.Equal(EnemyCell.Sunk, view.EnemyBoard.Cells[4][1]);
    }

    [Fact]
    public async Task EnemyBoard_OfTheOtherPlayer_IsNotAffected()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        var bobView = await bob.ViewAsync();

        Assert.Equal(0, CountEnemy(bobView, EnemyCell.Sunk));
        Assert.Equal(0, CountEnemy(bobView, EnemyCell.Hit));
    }

    [Fact]
    public async Task OwnBoard_OfTheVictim_KeepsHitCellsAndReportsTheSinkingInOwnShips()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        var view = await bob.ViewAsync();

        Assert.Equal(OwnCell.Hit, view.OwnBoard.Cells[4][0]);
        Assert.Equal(OwnCell.Hit, view.OwnBoard.Cells[4][1]);
        var destroyer = Assert.Single(view.OwnShips, ship => ship.Type == ShipType.Destroyer);
        Assert.True(destroyer.IsSunk);
    }

    [Fact]
    public async Task EnemyBoard_Json_AfterASinking_UsesOnlyTheFourKnownCellNames()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 0), (0, 4), (1, 4));

        using var document = JsonDocument.Parse(await (await ann.GetAsync()).Content.ReadAsStringAsync());

        var names = document.RootElement.GetProperty("enemyBoard").GetProperty("cells")
            .EnumerateArray()
            .SelectMany(row => row.EnumerateArray())
            .Select(cell => cell.GetString())
            .Distinct()
            .ToList();
        Assert.All(names, name => Assert.Contains(name, new[] { "Unknown", "Hit", "Miss", "Sunk" }));
        Assert.Contains("Sunk", names);
        Assert.DoesNotContain("Ship", names);
    }

    private static int CountEnemy(GameView view, EnemyCell kind) => view.EnemyBoard.Cells.Sum(row => row.Count(cell => cell == kind));
}
