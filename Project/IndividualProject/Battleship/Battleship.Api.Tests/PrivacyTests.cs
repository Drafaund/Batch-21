using System.Text.Json;

namespace Battleship.Api.Tests;

public class PrivacyTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http;

    public PrivacyTests(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public void EnemyCell_CannotDescribeAShip()
    {
        var names = Enum.GetNames<EnemyCell>();

        Assert.Equal(new[] { "Unknown", "Hit", "Miss", "Sunk" }, names);
    }

    [Fact]
    public async Task EnemyBoard_RightAfterCreation_IsCompletelyUnknownEvenThoughTheBotAlreadyPlacedItsShips()
    {
        var game = await GameClient.CreateAsync(_http);

        var view = await game.ViewAsync();

        Assert.All(view.EnemyBoard.Cells, row => Assert.All(row, cell => Assert.Equal(EnemyCell.Unknown, cell)));
    }

    [Fact]
    public async Task EnemyBoard_AfterShots_ShowsOnlyTheCellsTheFirerShotAt()
    {
        var game = await GameClient.CreateAsync(_http);
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
                Assert.Equal(fired.Contains((x, y)), shown);
            }
        }
    }

    [Fact]
    public async Task EnemyBoard_Json_ContainsNoShipValueAndOnlyTheFourKnownCellNames()
    {
        var game = await GameClient.CreateAsync(_http);
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
        Assert.All(enemyCells, name => Assert.Contains(name, new[] { "Unknown", "Hit", "Miss", "Sunk" }));
        Assert.DoesNotContain("Ship", enemyCells);
        Assert.DoesNotContain("Water", enemyCells);
    }

    [Fact]
    public async Task GameView_Json_HasNoEnemyShipListOrPositions()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceFleetAsync();

        using var document = JsonDocument.Parse(await (await game.GetAsync()).Content.ReadAsStringAsync());

        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(
            new[]
            {
                "code", "version", "phase", "you", "opponent", "yourTurn", "winner",
                "placement", "ownBoard", "ownShips", "enemyBoard", "enemyShipsRemaining", "endReason", "turnSecondsRemaining",
            },
            names);
        Assert.Equal(JsonValueKind.Number, document.RootElement.GetProperty("enemyShipsRemaining").ValueKind);
        var enemyBoardNames = document.RootElement.GetProperty("enemyBoard").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(new[] { "size", "cells" }, enemyBoardNames);
    }

    [Fact]
    public async Task ShotResponse_Json_DescribesOnlyTheShotAndTheViewAndTheBotReplies()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceFleetAsync();

        var response = await game.FireAsync(0, 0);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(new[] { "result", "botShots", "view" }, names);
    }

    [Fact]
    public async Task EnemyShipsRemaining_IsOnlyACountThatFallsWhenAShipSinks()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceFleetAsync();
        var remaining = (await game.ViewAsync()).EnemyShipsRemaining;
        Assert.Equal(5, remaining);

        var lowest = remaining;
        for (var i = 0; i < 100; i++)
        {
            var shot = await game.FireOkAsync(i % 10, i / 10);
            Assert.True(shot.View.EnemyShipsRemaining <= lowest);
            lowest = shot.View.EnemyShipsRemaining;
            if (shot.View.Phase == GamePhase.Finished)
            {
                break;
            }
        }
    }
}
