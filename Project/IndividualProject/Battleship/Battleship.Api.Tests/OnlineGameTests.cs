using System.Net;
using System.Text.Json;
using Battleship.Core;

namespace Battleship.Api.Tests;

public class OnlineGameTests : ApiTestFixture<ApiFactory>
{
    private async Task<(GameClient Ann, GameClient Bob)> NewRoomAsync()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");
        var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");
        return (ann, bob);
    }

    private async Task<(GameClient Ann, GameClient Bob)> NewBattleAsync()
    {
        var (ann, bob) = await NewRoomAsync();
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        return (ann, bob);
    }

    [Test]
    public async Task Join_OpenRoom_GivesSeat2ADifferentTokenAndMovesBothToPlacing()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");

        var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");

        Assert.That(bob.Seat, Is.EqualTo(2));
        Assert.That(bob.Token, Is.Not.EqualTo(ann.Token));
        Assert.That(bob.InitialView.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(bob.InitialView.You.Name, Is.EqualTo("Bob"));
        Assert.That(bob.InitialView.Opponent?.Name, Is.EqualTo("Ann"));
        var annView = await ann.ViewAsync();
        Assert.That(annView.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(annView.Opponent?.Name, Is.EqualTo("Bob"));
        Assert.That(annView.Opponent!.Connected, Is.False);
        Assert.That(annView.Version > ann.InitialView.Version, Is.True);
    }

    [Test]
    public async Task Join_CodeInLowerCaseWithSpaces_StillFindsTheRoom()
    {
        var ann = await GameClient.CreateRoomAsync(Http);

        var response = await GameClient.JoinRawAsync(Http, $" {ann.Code.ToLowerInvariant()} ".Trim(), new { playerName = "Bob" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Join_FullRoom_Returns409AndKeepsTheFirstGuest()
    {
        var (ann, bob) = await NewRoomAsync();

        var response = await GameClient.JoinRawAsync(Http, ann.Code, new { playerName = "Eve" });

        await GameClient.AssertProblemAsync(response, 409);
        Assert.That((await ann.ViewAsync()).Opponent?.Name, Is.EqualTo("Bob"));
        Assert.That((await bob.GetAsync()).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Join_GameAgainstABot_Returns409()
    {
        var game = await GameClient.CreateAsync(Http, opponent: "Random");

        var response = await GameClient.JoinRawAsync(Http, game.Code, new { playerName = "Eve" });

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task Join_UnknownCode_Returns404()
    {
        var response = await GameClient.JoinRawAsync(Http, "ZZZZZ", new { playerName = "Eve" });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("a name that is far too long")]
    public async Task Join_BadName_Returns400AndLeavesTheRoomOpen(string name)
    {
        var ann = await GameClient.CreateRoomAsync(Http);

        var response = await GameClient.JoinRawAsync(Http, ann.Code, new { playerName = name });

        await GameClient.AssertProblemAsync(response, 400);
        Assert.That((await ann.ViewAsync()).Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public async Task Join_MissingBody_Returns400()
    {
        var ann = await GameClient.CreateRoomAsync(Http);

        var response = await GameClient.JoinRawAsync(Http, ann.Code, null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Test]
    public async Task Lobby_PlacingShipsOrFiringBeforeSomeoneJoined_Returns409()
    {
        var ann = await GameClient.CreateRoomAsync(Http);

        await GameClient.AssertProblemAsync(await ann.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal), 409);
        await GameClient.AssertProblemAsync(await ann.PlaceRandomAsync(), 409);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
    }

    [Test]
    public async Task Placing_EachPlayerPlacesOnlyOnTheirOwnBoard()
    {
        var (ann, bob) = await NewRoomAsync();

        var response = await ann.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        response.EnsureSuccessStatusCode();
        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.That(annView.Placement.Placed, Is.EqualTo(new[] { ShipType.Carrier }));
        Assert.That(bobView.Placement.Placed, Is.Empty);
        Assert.That(bobView.OwnShips, Is.Empty);
        Assert.That(bobView.Phase, Is.EqualTo(GamePhase.Placing));
    }

    [Test]
    public async Task Placing_OneFleetComplete_WaitsForTheOtherBeforeTheGameStarts()
    {
        var (ann, bob) = await NewRoomAsync();

        var annView = await ann.PlaceFleetAsync();

        Assert.That(annView.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(annView.Placement.Remaining, Is.Empty);
        Assert.That(annView.YourTurn, Is.False);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
        Assert.That((await bob.ViewAsync()).Phase, Is.EqualTo(GamePhase.Placing));
    }

    [Test]
    public async Task Placing_BothFleetsComplete_StartsTheGameWithSeat1ToFire()
    {
        var (ann, bob) = await NewBattleAsync();

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();

        Assert.That(annView.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(bobView.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(annView.YourTurn, Is.True);
        Assert.That(bobView.YourTurn, Is.False);
    }

    [Test]
    public async Task Placing_RandomForBothPlayers_StartsTheGame()
    {
        var (ann, bob) = await NewRoomAsync();

        (await ann.PlaceRandomAsync()).EnsureSuccessStatusCode();
        (await bob.PlaceRandomAsync()).EnsureSuccessStatusCode();

        Assert.That((await bob.ViewAsync()).Phase, Is.EqualTo(GamePhase.InProgress));
    }

    [Test]
    public async Task Fire_PlayerWhoseTurnItIsNot_Returns409AndChangesNothing()
    {
        var (ann, bob) = await NewBattleAsync();
        var before = await bob.ViewAsync();

        var response = await bob.FireAsync(0, 0);

        await GameClient.AssertProblemAsync(response, 409);
        Assert.That((await bob.ViewAsync()).Version, Is.EqualTo(before.Version));
        Assert.That((await ann.ViewAsync()).YourTurn, Is.True);
    }

    [Test]
    public async Task Fire_ValidShot_PassesTheTurnToTheOtherPlayerAndAnswersWithoutBotShots()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await ann.FireOkAsync(0, 0);

        Assert.That(shot.Result, Is.EqualTo(ShotResult.Hit));
        Assert.That(shot.BotShots, Is.Empty);
        Assert.That(shot.View.YourTurn, Is.False);
        Assert.That((await bob.ViewAsync()).YourTurn, Is.True);
    }

    [Test]
    public async Task Fire_InvalidShot_KeepsTheTurn()
    {
        var (ann, _) = await NewBattleAsync();

        var shot = await ann.FireOkAsync(20, 20);

        Assert.That(shot.Result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(shot.View.YourTurn, Is.True);
    }

    [Test]
    public async Task Fire_ShowsTheShotOnTheVictimsOwnBoardAndOnTheShootersEnemyBoard()
    {
        var (ann, bob) = await NewBattleAsync();

        await ann.FireOkAsync(0, 0);
        await bob.FireOkAsync(9, 9);

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.That(annView.OwnBoard.Cells[9][9], Is.EqualTo(OwnCell.Miss));
        Assert.That(annView.EnemyBoard.Cells[0][0], Is.EqualTo(EnemyCell.Hit));
        Assert.That(bobView.OwnBoard.Cells[0][0], Is.EqualTo(OwnCell.Hit));
        Assert.That(bobView.EnemyBoard.Cells[9][9], Is.EqualTo(EnemyCell.Miss));
    }

    [Test]
    public async Task Views_NeverContainTheOtherPlayersShipsOrToken()
    {
        var (ann, bob) = await NewBattleAsync();
        await ann.FireOkAsync(9, 9);
        await bob.FireOkAsync(9, 9);

        foreach (var (viewer, other) in new[] { (ann, bob), (bob, ann) })
        {
            using var document = JsonDocument.Parse(await (await viewer.GetAsync()).Content.ReadAsStringAsync());
            var json = document.RootElement.GetRawText();
            Assert.That(json, Does.Not.Contain(other.Token));
            var enemy = document.RootElement.GetProperty("enemyBoard").GetRawText();
            Assert.That(enemy, Does.Not.Contain("Ship"));
        }
    }

    [Test]
    public async Task Token_BelongsToOneSeat_SoAnnCannotFireAgainOnBobsTurn()
    {
        var (ann, _) = await NewBattleAsync();
        await ann.FireOkAsync(0, 0);

        var response = await ann.FireAsync(1, 0);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Test]
    public async Task Token_FromAnotherRoom_Returns401()
    {
        var (ann, _) = await NewRoomAsync();
        var (other, _) = await NewRoomAsync();

        var response = await ann.GetAsync(other.Token);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Test]
    public async Task FullGame_TwoPlayersTakingTurns_EndsWithOneWinnerAndOneLoser()
    {
        var (ann, bob) = await NewBattleAsync();

        ShotResponse? last = null;
        var annShots = 0;
        for (var y = 0; y < 10 && last?.View.Phase != GamePhase.Finished; y++)
        {
            for (var x = 0; x < 10 && last?.View.Phase != GamePhase.Finished; x++)
            {
                last = await ann.FireOkAsync(x, y);
                annShots++;
                if (last.View.Phase != GamePhase.Finished)
                {
                    var bobShot = await bob.FireOkAsync(9 - (annShots - 1) % 10, 9 - (annShots - 1) / 10);
                    Assert.That(bobShot.Result, Is.Not.EqualTo(ShotResult.Invalid));
                }
            }
        }

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.That(annView.Phase, Is.EqualTo(GamePhase.Finished));
        Assert.That(annView.Winner, Is.EqualTo("you"));
        Assert.That(bobView.Winner, Is.EqualTo("opponent"));
        Assert.That(annView.EnemyShipsRemaining, Is.EqualTo(0));
        Assert.That(annView.YourTurn, Is.False);
        Assert.That(bobView.YourTurn, Is.False);
        Assert.That(annView.EndReason, Is.Null);
        await GameClient.AssertProblemAsync(await bob.FireAsync(5, 5), 409);
    }
}
