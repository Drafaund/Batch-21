using System.Text.Json;

namespace Battleship.Api.Tests;

public class PrivacyTests : ApiTestFixture<ApiFactory>
{
    [Test]
    public void EnemyCell_CannotDescribeAShip()
    {
        var names = Enum.GetNames<EnemyCell>();

        Assert.That(names, Is.EqualTo(new[] { "Unknown", "Hit", "Miss", "Sunk" }));
    }

    [Test]
    public async Task EnemyBoard_RightAfterCreation_IsCompletelyUnknownEvenThoughTheBotAlreadyPlacedItsShips()
    {
        var game = await GameClient.CreateAsync(Http);

        var view = await game.ViewAsync();

        Assert.That(view.EnemyBoard.Cells.SelectMany(row => row), Is.All.EqualTo(EnemyCell.Unknown));
    }

    [Test]
    public async Task EnemyBoard_AfterShots_ShowsOnlyTheCellsTheFirerShotAt()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceFleetAsync();
        var fired = new HashSet<(int X, int Y)>();
        GameView view = await game.ViewAsync();

        for (var i = 0; i < 40; i++)
        {
            var (x, y) = (i % 10, i / 10);
            var shot = await game.FireOkAsync(x, y);
            fired.Add((x, y));
            view = shot.View;
            if (view.Phase == GamePhase.Finished)
            {
                break;
            }
        }

        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                var shown = view.EnemyBoard.Cells[y][x] != EnemyCell.Unknown;
                Assert.That(shown, Is.EqualTo(fired.Contains((x, y))));
            }
        }
    }

    [Test]
    public async Task EnemyBoard_Json_ContainsNoShipValueAndOnlyTheFourKnownCellNames()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceFleetAsync();
        await game.FireOkAsync(0, 0);
        await game.FireOkAsync(1, 0);

        using var document = JsonDocument.Parse(await (await game.GetAsync()).Content.ReadAsStringAsync());

        var enemyCells = document.RootElement.GetProperty("enemyBoard").GetProperty("cells")
            .EnumerateArray()
            .SelectMany(row => row.EnumerateArray())
            .Select(cell => cell.GetString())
            .Distinct()
            .ToList();
        Assert.That(enemyCells, Is.All.AnyOf("Unknown", "Hit", "Miss", "Sunk"));
        Assert.That(enemyCells, Does.Not.Contain("Ship"));
        Assert.That(enemyCells, Does.Not.Contain("Water"));
    }

    [Test]
    public async Task GameView_Json_HasNoEnemyShipListOrPositions()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceFleetAsync();

        using var document = JsonDocument.Parse(await (await game.GetAsync()).Content.ReadAsStringAsync());

        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.That(names, Is.EqualTo(new[]
            {
                "code", "version", "phase", "you", "opponent", "yourTurn", "winner",
                "placement", "ownBoard", "ownShips", "enemyBoard", "enemyShipsRemaining", "endReason", "turnSecondsRemaining",
            }));
        Assert.That(document.RootElement.GetProperty("enemyShipsRemaining").ValueKind, Is.EqualTo(JsonValueKind.Number));
        var enemyBoardNames = document.RootElement.GetProperty("enemyBoard").EnumerateObject().Select(p => p.Name).ToList();
        Assert.That(enemyBoardNames, Is.EqualTo(new[] { "size", "cells" }));
    }

    [Test]
    public async Task ShotResponse_Json_DescribesOnlyTheShotAndTheViewAndTheBotReplies()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceFleetAsync();

        var response = await game.FireAsync(0, 0);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.That(names, Is.EqualTo(new[] { "result", "botShots", "view" }));
    }

    [Test]
    public async Task EnemyShipsRemaining_IsOnlyACountThatFallsWhenAShipSinks()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceFleetAsync();
        var remaining = (await game.ViewAsync()).EnemyShipsRemaining;
        Assert.That(remaining, Is.EqualTo(5));

        var lowest = remaining;
        for (var i = 0; i < 100; i++)
        {
            var shot = await game.FireOkAsync(i % 10, i / 10);
            Assert.That(shot.View.EnemyShipsRemaining <= lowest, Is.True);
            lowest = shot.View.EnemyShipsRemaining;
            if (shot.View.Phase == GamePhase.Finished)
            {
                break;
            }
        }
    }
}
