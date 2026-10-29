using System.Net.Http.Json;
using Battleship.Core;

namespace Battleship.Api.Tests;

public class PlaceShipTests : ApiTestFixture<ApiFactory>
{
    private static int CountOwn(GameView view, OwnCell kind) => view.OwnBoard.Cells.Sum(row => row.Count(cell => cell == kind));

    [Test]
    public async Task PlaceShip_ValidHorizontal_FillsShipBoardAndPlacement()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PlaceShipAsync(ShipType.Cruiser, 2, 4, Orientation.Horizontal);

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var view = await GameClient.ReadAsync<GameView>(response);
        Assert.That(view.OwnShips, Has.Exactly(1).Items);
        var ship = view.OwnShips.Single();
        Assert.That(ship.Type, Is.EqualTo(ShipType.Cruiser));
        Assert.That(ship.Length, Is.EqualTo(3));
        Assert.That(ship.Orientation, Is.EqualTo(Orientation.Horizontal));
        Assert.That(ship.Positions, Is.EqualTo(new[] { new CoordinateDto(2, 4), new CoordinateDto(3, 4), new CoordinateDto(4, 4) }));
        Assert.That(CountOwn(view, OwnCell.Ship), Is.EqualTo(3));
        Assert.That(view.OwnBoard.Cells[4][2], Is.EqualTo(OwnCell.Ship));
        Assert.That(view.OwnBoard.Cells[4][4], Is.EqualTo(OwnCell.Ship));
        Assert.That(view.Placement.Placed, Is.EqualTo(new[] { ShipType.Cruiser }));
        Assert.That(view.Placement.Remaining.Count, Is.EqualTo(4));
        Assert.That(view.Placement.Remaining, Does.Not.Contain(ShipType.Cruiser));
        Assert.That(view.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(view.YourTurn, Is.False);
    }

    [Test]
    public async Task PlaceShip_ValidVertical_ExtendsDownward()
    {
        var game = await GameClient.CreateAsync(Http);

        var view = await GameClient.ReadAsync<GameView>(await game.PlaceShipAsync(ShipType.Destroyer, 7, 8, Orientation.Vertical));

        Assert.That(view.OwnShips[0].Positions, Is.EqualTo(new[] { new CoordinateDto(7, 8), new CoordinateDto(7, 9) }));
        Assert.That(view.OwnBoard.Cells[9][7], Is.EqualTo(OwnCell.Ship));
    }

    [Test]
    public async Task PlaceShip_Valid_IncreasesTheVersion()
    {
        var game = await GameClient.CreateAsync(Http);

        var view = await GameClient.ReadAsync<GameView>(await game.PlaceShipAsync(ShipType.Destroyer, 0, 0, Orientation.Horizontal));

        Assert.That(view.Version, Is.EqualTo(game.InitialView.Version + 1));
    }

    [Test]
    public async Task PlaceShip_ShipEndingOnTheLastCell_IsAccepted()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PlaceShipAsync(ShipType.Carrier, 5, 9, Orientation.Horizontal);

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [TestCase(6, 0, Orientation.Horizontal)]
    [TestCase(0, 6, Orientation.Vertical)]
    [TestCase(-1, 0, Orientation.Horizontal)]
    [TestCase(0, -1, Orientation.Vertical)]
    [TestCase(10, 0, Orientation.Vertical)]
    [TestCase(0, 10, Orientation.Horizontal)]
    public async Task PlaceShip_PartlyOutsideTheBoard_Returns409AndChangesNothing(int x, int y, Orientation orientation)
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PlaceShipAsync(ShipType.Carrier, x, y, orientation);

        await GameClient.AssertProblemAsync(response, 409);
        var view = await game.ViewAsync();
        Assert.That(view.OwnShips, Is.Empty);
        Assert.That(view.Version, Is.EqualTo(game.InitialView.Version));
        Assert.That(CountOwn(view, OwnCell.Ship), Is.EqualTo(0));
    }

    [Test]
    public async Task PlaceShip_OverlappingAnotherShip_Returns409()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceShipAsync(ShipType.Destroyer, 4, 0, Orientation.Vertical);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task PlaceShip_TouchingAnotherShipWithoutOverlap_IsAccepted()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceShipAsync(ShipType.Destroyer, 5, 0, Orientation.Vertical);

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [Test]
    public async Task PlaceShip_ShipTypeAlreadyPlaced_Returns409()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceShipAsync(ShipType.Cruiser, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceShipAsync(ShipType.Cruiser, 0, 5, Orientation.Horizontal);

        await GameClient.AssertProblemAsync(response, 409);
        Assert.That((await game.ViewAsync()).OwnShips, Has.Exactly(1).Items);
    }

    [Test]
    public async Task PlaceShip_FifthShip_StartsTheGameAndGivesTheFirstTurnToThePlayer()
    {
        var game = await GameClient.CreateAsync(Http);

        var view = await game.PlaceFleetAsync();

        Assert.That(view.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(view.YourTurn, Is.True);
        Assert.That(view.OwnShips.Count, Is.EqualTo(5));
        Assert.That(view.Placement.Remaining, Is.Empty);
        Assert.That(CountOwn(view, OwnCell.Ship), Is.EqualTo(17));
    }

    [Test]
    public async Task PlaceShip_FourShips_GameStaysInPlacing()
    {
        var game = await GameClient.CreateAsync(Http);
        foreach (var type in new[] { ShipType.Carrier, ShipType.Battleship, ShipType.Cruiser, ShipType.Submarine })
        {
            await game.PlaceShipAsync(type, 0, (int)type, Orientation.Horizontal);
        }

        var view = await game.ViewAsync();

        Assert.That(view.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(view.YourTurn, Is.False);
    }

    [Test]
    public async Task PlaceShip_AfterTheGameStarted_Returns409()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceFleetAsync();

        var response = await game.PlaceShipAsync(ShipType.Destroyer, 0, 9, Orientation.Horizontal);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task PlaceShip_EmptyJsonObject_Returns400()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PostRawAsync("ships", new { });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task PlaceShip_NoBody_Returns400()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PostRawAsync("ships", null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [TestCase("Dreadnought", "Horizontal")]
    [TestCase("Carrier", "Diagonal")]
    [TestCase(0, "Horizontal")]
    [TestCase("Carrier", 1)]
    public async Task PlaceShip_UnknownOrNumericEnum_Returns400(object type, object orientation)
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PostRawAsync("ships", new { type, start = new { x = 0, y = 0 }, orientation });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task PlaceShip_MissingStart_Returns400()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PostRawAsync("ships", new { type = "Carrier", orientation = "Horizontal" });

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task PlaceShip_NoToken_Returns401()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PlaceShipAsync(null, ShipType.Carrier, 0, 0, Orientation.Horizontal);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task PlaceShip_UnknownCode_Returns404()
    {
        var response = await GameClient.SendAsync(
            Http,
            HttpMethod.Post,
            "/api/games/ZZZZZ/ships",
            "any",
            new { type = "Carrier", start = new { x = 0, y = 0 }, orientation = "Horizontal" });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Test]
    public async Task PlaceRandom_NoShipsYet_PlacesAllFiveAndStartsTheGame()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PlaceRandomAsync();

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var view = await GameClient.ReadAsync<GameView>(response);
        Assert.That(view.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(view.YourTurn, Is.True);
        Assert.That(view.OwnShips.Count, Is.EqualTo(5));
        Assert.That(CountOwn(view, OwnCell.Ship), Is.EqualTo(17));
        Assert.That(view.OwnShips.Select(s => s.Type).OrderBy(t => t), Is.EqualTo(Enum.GetValues<ShipType>().OrderBy(t => t)));
    }

    [Test]
    public async Task PlaceRandom_Placement_StaysInsideTheBoardWithoutOverlap()
    {
        for (var i = 0; i < 10; i++)
        {
            var game = await GameClient.CreateAsync(Http);

            var view = await GameClient.ReadAsync<GameView>(await game.PlaceRandomAsync());

            var cells = view.OwnShips.SelectMany(s => s.Positions).ToList();
            Assert.That(cells.Distinct().Count(), Is.EqualTo(17));
            Assert.That(cells.Select(c => c.X), Is.All.InRange(0, 9));
            Assert.That(cells.Select(c => c.Y), Is.All.InRange(0, 9));
        }
    }

    [Test]
    public async Task PlaceRandom_AfterAShipWasPlaced_Returns409()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        var response = await game.PlaceRandomAsync();

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task PlaceRandom_Twice_SecondCallReturns409()
    {
        var game = await GameClient.CreateAsync(Http);
        await game.PlaceRandomAsync();

        var response = await game.PlaceRandomAsync();

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task PlaceRandom_NoToken_Returns401()
    {
        var game = await GameClient.CreateAsync(Http);

        var response = await game.PlaceRandomAsync(null);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task PlaceShip_OverTheWire_IsPostedAsJson()
    {
        var game = await GameClient.CreateAsync(Http);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{game.Code}/ships");
        request.Headers.Add(GameClient.TokenHeader, game.Token);
        request.Content = new StringContent(
            "{\"type\":\"Submarine\",\"start\":{\"x\":1,\"y\":1},\"orientation\":\"Vertical\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await Http.SendAsync(request);

        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That((await GameClient.ReadAsync<GameView>(response)).OwnShips[0].Type, Is.EqualTo(ShipType.Submarine));
    }
}
