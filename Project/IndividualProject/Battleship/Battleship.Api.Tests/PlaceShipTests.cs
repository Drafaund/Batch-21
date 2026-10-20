using System.Net.Http.Json;
using Battleship.Core;

namespace Battleship.Api.Tests;

public class PlaceShipTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http;

    public PlaceShipTests(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    private static int CountOwn(GameView view, OwnCell kind) => view.OwnBoard.Cells.Sum(row => row.Count(cell => cell == kind));

    [Fact]
    public async Task PlaceShip_ValidHorizontal_FillsShipBoardAndPlacement()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PlaceShipAsync(ShipType.Cruiser, 2, 4, Orientation.Horizontal);

        Assert.True(response.IsSuccessStatusCode);
        var view = await GameClient.ReadAsync<GameView>(response);
        var ship = Assert.Single(view.OwnShips);
        Assert.Equal(ShipType.Cruiser, ship.Type);
        Assert.Equal(3, ship.Length);
        Assert.Equal(Orientation.Horizontal, ship.Orientation);
        Assert.Equal(new[] { new CoordinateDto(2, 4), new CoordinateDto(3, 4), new CoordinateDto(4, 4) }, ship.Positions);
        Assert.Equal(3, CountOwn(view, OwnCell.Ship));
        Assert.Equal(OwnCell.Ship, view.OwnBoard.Cells[4][2]);
        Assert.Equal(OwnCell.Ship, view.OwnBoard.Cells[4][4]);
        Assert.Equal(new[] { ShipType.Cruiser }, view.Placement.Placed);
        Assert.Equal(4, view.Placement.Remaining.Count);
        Assert.DoesNotContain(ShipType.Cruiser, view.Placement.Remaining);
        Assert.Equal(GamePhase.Placing, view.Phase);
        Assert.False(view.YourTurn);
    }

    [Fact]
    public async Task PlaceShip_ValidVertical_ExtendsDownward()
    {
        var game = await GameClient.CreateAsync(_http);

        var view = await GameClient.ReadAsync<GameView>(await game.PlaceShipAsync(ShipType.Destroyer, 7, 8, Orientation.Vertical));

        Assert.Equal(new[] { new CoordinateDto(7, 8), new CoordinateDto(7, 9) }, view.OwnShips[0].Positions);
        Assert.Equal(OwnCell.Ship, view.OwnBoard.Cells[9][7]);
    }

    [Fact]
    public async Task PlaceShip_Valid_IncreasesTheVersion()
    {
        var game = await GameClient.CreateAsync(_http);

        var view = await GameClient.ReadAsync<GameView>(await game.PlaceShipAsync(ShipType.Destroyer, 0, 0, Orientation.Horizontal));

        Assert.Equal(game.InitialView.Version + 1, view.Version);
    }

    [Fact]
    public async Task PlaceShip_ShipEndingOnTheLastCell_IsAccepted()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PlaceShipAsync(ShipType.Carrier, 5, 9, Orientation.Horizontal);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [InlineData(6, 0, Orientation.Horizontal)]
    [InlineData(0, 6, Orientation.Vertical)]
    [InlineData(-1, 0, Orientation.Horizontal)]
    [InlineData(0, -1, Orientation.Vertical)]
    [InlineData(10, 0, Orientation.Vertical)]
    [InlineData(0, 10, Orientation.Horizontal)]
    public async Task PlaceShip_PartlyOutsideTheBoard_Returns409AndChangesNothing(int x, int y, Orientation orientation)
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PlaceShipAsync(ShipType.Carrier, x, y, orientation);

        await GameClient.AssertProblemAsync(response, 409);
        var view = await game.ViewAsync();
        Assert.Empty(view.OwnShips);
        Assert.Equal(game.InitialView.Version, view.Version);
        Assert.Equal(0, CountOwn(view, OwnCell.Ship));
    }

    [Fact]
    public async Task PlaceShip_OverlappingAnotherShip_Returns409()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceShipAsync(ShipType.Destroyer, 4, 0, Orientation.Vertical);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task PlaceShip_TouchingAnotherShipWithoutOverlap_IsAccepted()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceShipAsync(ShipType.Destroyer, 5, 0, Orientation.Vertical);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PlaceShip_ShipTypeAlreadyPlaced_Returns409()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceShipAsync(ShipType.Cruiser, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceShipAsync(ShipType.Cruiser, 0, 5, Orientation.Horizontal);

        await GameClient.AssertProblemAsync(response, 409);
        Assert.Single((await game.ViewAsync()).OwnShips);
    }

    [Fact]
    public async Task PlaceShip_FifthShip_StartsTheGameAndGivesTheFirstTurnToThePlayer()
    {
        var game = await GameClient.CreateAsync(_http);

        var view = await game.PlaceFleetAsync();

        Assert.Equal(GamePhase.InProgress, view.Phase);
        Assert.True(view.YourTurn);
        Assert.Equal(5, view.OwnShips.Count);
        Assert.Empty(view.Placement.Remaining);
        Assert.Equal(17, CountOwn(view, OwnCell.Ship));
    }

    [Fact]
    public async Task PlaceShip_FourShips_GameStaysInPlacing()
    {
        var game = await GameClient.CreateAsync(_http);
        foreach (var type in new[] { ShipType.Carrier, ShipType.Battleship, ShipType.Cruiser, ShipType.Submarine })
        {
            await game.PlaceShipAsync(type, 0, (int)type, Orientation.Horizontal);
        }

        var view = await game.ViewAsync();

        Assert.Equal(GamePhase.Placing, view.Phase);
        Assert.False(view.YourTurn);
    }

    [Fact]
    public async Task PlaceShip_AfterTheGameStarted_Returns409()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceFleetAsync();

        var response = await game.PlaceShipAsync(ShipType.Destroyer, 0, 9, Orientation.Horizontal);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task PlaceShip_EmptyJsonObject_Returns400()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PostRawAsync("ships", new { });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task PlaceShip_NoBody_Returns400()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PostRawAsync("ships", null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Theory]
    [InlineData("Dreadnought", "Horizontal")]
    [InlineData("Carrier", "Diagonal")]
    [InlineData(0, "Horizontal")]
    [InlineData("Carrier", 1)]
    public async Task PlaceShip_UnknownOrNumericEnum_Returns400(object type, object orientation)
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PostRawAsync("ships", new { type, start = new { x = 0, y = 0 }, orientation });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task PlaceShip_MissingStart_Returns400()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PostRawAsync("ships", new { type = "Carrier", orientation = "Horizontal" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task PlaceShip_NoToken_Returns401()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PlaceShipAsync(null, ShipType.Carrier, 0, 0, Orientation.Horizontal);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
    public async Task PlaceShip_UnknownCode_Returns404()
    {
        var response = await GameClient.SendAsync(
            _http,
            HttpMethod.Post,
            "/api/games/ZZZZZ/ships",
            "any",
            new { type = "Carrier", start = new { x = 0, y = 0 }, orientation = "Horizontal" });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Fact]
    public async Task PlaceRandom_NoShipsYet_PlacesAllFiveAndStartsTheGame()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PlaceRandomAsync();

        Assert.True(response.IsSuccessStatusCode);
        var view = await GameClient.ReadAsync<GameView>(response);
        Assert.Equal(GamePhase.InProgress, view.Phase);
        Assert.True(view.YourTurn);
        Assert.Equal(5, view.OwnShips.Count);
        Assert.Equal(17, CountOwn(view, OwnCell.Ship));
        Assert.Equal(Enum.GetValues<ShipType>().OrderBy(t => t), view.OwnShips.Select(s => s.Type).OrderBy(t => t));
    }

    [Fact]
    public async Task PlaceRandom_Placement_StaysInsideTheBoardWithoutOverlap()
    {
        for (var i = 0; i < 10; i++)
        {
            var game = await GameClient.CreateAsync(_http);

            var view = await GameClient.ReadAsync<GameView>(await game.PlaceRandomAsync());

            var cells = view.OwnShips.SelectMany(s => s.Positions).ToList();
            Assert.Equal(17, cells.Distinct().Count());
            Assert.All(cells, c => Assert.InRange(c.X, 0, 9));
            Assert.All(cells, c => Assert.InRange(c.Y, 0, 9));
        }
    }

    [Fact]
    public async Task PlaceRandom_AfterAShipWasPlaced_Returns409()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceRandomAsync();

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task PlaceRandom_Twice_SecondCallReturns409()
    {
        var game = await GameClient.CreateAsync(_http);
        await game.PlaceRandomAsync();

        var response = await game.PlaceRandomAsync();

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task PlaceRandom_NoToken_Returns401()
    {
        var game = await GameClient.CreateAsync(_http);

        var response = await game.PlaceRandomAsync(null);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
    public async Task PlaceShip_OverTheWire_IsPostedAsJson()
    {
        var game = await GameClient.CreateAsync(_http);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{game.Code}/ships");
        request.Headers.Add(GameClient.TokenHeader, game.Token);
        request.Content = new StringContent(
            "{\"type\":\"Submarine\",\"start\":{\"x\":1,\"y\":1},\"orientation\":\"Vertical\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _http.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(ShipType.Submarine, (await GameClient.ReadAsync<GameView>(response)).OwnShips[0].Type);
    }
}
