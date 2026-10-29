using System.Text.Json;
using Battleship.Core;

namespace Battleship.Api.Tests;

public class SunkCellTests : ApiTestFixture<ApiFactory>
{
    private async Task<(GameClient Ann, GameClient Bob)> NewBattleAsync()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");
        var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        return (ann, bob);
    }

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

    [Test]
    public async Task EnemyBoard_ShipHitButNotSunk_ShowsHit()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await AnnFiresAsync(ann, bob, (0, 4));

        Assert.That(shot.Result, Is.EqualTo(ShotResult.Hit));
        Assert.That(shot.View.EnemyBoard.Cells[4][0], Is.EqualTo(EnemyCell.Hit));
        Assert.That(CountEnemy(shot.View, EnemyCell.Sunk), Is.EqualTo(0));
    }

    [Test]
    public async Task EnemyBoard_ShipThatSinks_ShowsEveryCellOfThatShipAsSunk()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        Assert.That(shot.Result, Is.EqualTo(ShotResult.Sunk));
        Assert.That(shot.View.EnemyBoard.Cells[4][0], Is.EqualTo(EnemyCell.Sunk));
        Assert.That(shot.View.EnemyBoard.Cells[4][1], Is.EqualTo(EnemyCell.Sunk));
        Assert.That(CountEnemy(shot.View, EnemyCell.Sunk), Is.EqualTo(2));
        Assert.That(CountEnemy(shot.View, EnemyCell.Hit), Is.EqualTo(0));
    }

    [Test]
    public async Task EnemyBoard_ShipThatSinks_LeavesOtherShipsHitAndEverythingElseUnknown()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await AnnFiresAsync(ann, bob, (0, 0), (0, 4), (1, 4));

        Assert.That(shot.View.EnemyBoard.Cells[0][0], Is.EqualTo(EnemyCell.Hit));
        Assert.That(CountEnemy(shot.View, EnemyCell.Hit), Is.EqualTo(1));
        Assert.That(CountEnemy(shot.View, EnemyCell.Sunk), Is.EqualTo(2));
        Assert.That(CountEnemy(shot.View, EnemyCell.Unknown), Is.EqualTo(97));
        Assert.That(shot.View.EnemyBoard.Cells[4][2], Is.EqualTo(EnemyCell.Unknown));
    }

    [Test]
    public async Task EnemyBoard_AfterTheSinking_StaysSunkWhenTheViewIsLoadedAgain()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        var view = await ann.ViewAsync();

        Assert.That(view.EnemyBoard.Cells[4][0], Is.EqualTo(EnemyCell.Sunk));
        Assert.That(view.EnemyBoard.Cells[4][1], Is.EqualTo(EnemyCell.Sunk));
    }

    [Test]
    public async Task EnemyBoard_OfTheOtherPlayer_IsNotAffected()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        var bobView = await bob.ViewAsync();

        Assert.That(CountEnemy(bobView, EnemyCell.Sunk), Is.EqualTo(0));
        Assert.That(CountEnemy(bobView, EnemyCell.Hit), Is.EqualTo(0));
    }

    [Test]
    public async Task OwnBoard_OfTheVictim_KeepsHitCellsAndReportsTheSinkingInOwnShips()
    {
        var (ann, bob) = await NewBattleAsync();
        await AnnFiresAsync(ann, bob, (0, 4), (1, 4));

        var view = await bob.ViewAsync();

        Assert.That(view.OwnBoard.Cells[4][0], Is.EqualTo(OwnCell.Hit));
        Assert.That(view.OwnBoard.Cells[4][1], Is.EqualTo(OwnCell.Hit));
        var destroyer = view.OwnShips.Single(ship => ship.Type == ShipType.Destroyer);
        Assert.That(destroyer.IsSunk, Is.True);
    }

    [Test]
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
        Assert.That(names, Is.All.AnyOf("Unknown", "Hit", "Miss", "Sunk"));
        Assert.That(names, Does.Contain("Sunk"));
        Assert.That(names, Does.Not.Contain("Ship"));
    }

    private static int CountEnemy(GameView view, EnemyCell kind) => view.EnemyBoard.Cells.Sum(row => row.Count(cell => cell == kind));
}
