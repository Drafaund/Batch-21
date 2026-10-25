using System.Net;
using System.Text.Json;
using Battleship.Core;

namespace Battleship.Api.Tests;

public class OnlineGameTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http;

    public OnlineGameTests(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    private async Task<(GameClient Ann, GameClient Bob)> NewRoomAsync()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");
        var bob = await GameClient.JoinAsync(_http, ann.Code, "Bob");
        return (ann, bob);
    }

    private async Task<(GameClient Ann, GameClient Bob)> NewBattleAsync()
    {
        var (ann, bob) = await NewRoomAsync();
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        return (ann, bob);
    }

    [Fact]
    public async Task Join_OpenRoom_GivesSeat2ADifferentTokenAndMovesBothToPlacing()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");

        var bob = await GameClient.JoinAsync(_http, ann.Code, "Bob");

        Assert.Equal(2, bob.Seat);
        Assert.NotEqual(ann.Token, bob.Token);
        Assert.Equal(GamePhase.Placing, bob.InitialView.Phase);
        Assert.Equal("Bob", bob.InitialView.You.Name);
        Assert.Equal("Ann", bob.InitialView.Opponent?.Name);
        var annView = await ann.ViewAsync();
        Assert.Equal(GamePhase.Placing, annView.Phase);
        Assert.Equal("Bob", annView.Opponent?.Name);
        Assert.False(annView.Opponent!.Connected);
        Assert.True(annView.Version > ann.InitialView.Version);
    }

    [Fact]
    public async Task Join_CodeInLowerCaseWithSpaces_StillFindsTheRoom()
    {
        var ann = await GameClient.CreateRoomAsync(_http);

        var response = await GameClient.JoinRawAsync(_http, $" {ann.Code.ToLowerInvariant()} ".Trim(), new { playerName = "Bob" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Join_FullRoom_Returns409AndKeepsTheFirstGuest()
    {
        var (ann, bob) = await NewRoomAsync();

        var response = await GameClient.JoinRawAsync(_http, ann.Code, new { playerName = "Eve" });

        await GameClient.AssertProblemAsync(response, 409);
        Assert.Equal("Bob", (await ann.ViewAsync()).Opponent?.Name);
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync()).StatusCode);
    }

    [Fact]
    public async Task Join_GameAgainstABot_Returns409()
    {
        var game = await GameClient.CreateAsync(_http, opponent: "Random");

        var response = await GameClient.JoinRawAsync(_http, game.Code, new { playerName = "Eve" });

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task Join_UnknownCode_Returns404()
    {
        var response = await GameClient.JoinRawAsync(_http, "ZZZZZ", new { playerName = "Eve" });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a name that is far too long")]
    public async Task Join_BadName_Returns400AndLeavesTheRoomOpen(string name)
    {
        var ann = await GameClient.CreateRoomAsync(_http);

        var response = await GameClient.JoinRawAsync(_http, ann.Code, new { playerName = name });

        await GameClient.AssertProblemAsync(response, 400);
        Assert.Equal(GamePhase.Lobby, (await ann.ViewAsync()).Phase);
    }

    [Fact]
    public async Task Join_MissingBody_Returns400()
    {
        var ann = await GameClient.CreateRoomAsync(_http);

        var response = await GameClient.JoinRawAsync(_http, ann.Code, null);

        await GameClient.AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task Lobby_PlacingShipsOrFiringBeforeSomeoneJoined_Returns409()
    {
        var ann = await GameClient.CreateRoomAsync(_http);

        await GameClient.AssertProblemAsync(await ann.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal), 409);
        await GameClient.AssertProblemAsync(await ann.PlaceRandomAsync(), 409);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
    }

    [Fact]
    public async Task Placing_EachPlayerPlacesOnlyOnTheirOwnBoard()
    {
        var (ann, bob) = await NewRoomAsync();

        var response = await ann.PlaceShipAsync(ShipType.Carrier, 0, 0, Orientation.Horizontal);

        response.EnsureSuccessStatusCode();
        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.Equal(new[] { ShipType.Carrier }, annView.Placement.Placed);
        Assert.Empty(bobView.Placement.Placed);
        Assert.Empty(bobView.OwnShips);
        Assert.Equal(GamePhase.Placing, bobView.Phase);
    }

    [Fact]
    public async Task Placing_OneFleetComplete_WaitsForTheOtherBeforeTheGameStarts()
    {
        var (ann, bob) = await NewRoomAsync();

        var annView = await ann.PlaceFleetAsync();

        Assert.Equal(GamePhase.Placing, annView.Phase);
        Assert.Empty(annView.Placement.Remaining);
        Assert.False(annView.YourTurn);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
        Assert.Equal(GamePhase.Placing, (await bob.ViewAsync()).Phase);
    }

    [Fact]
    public async Task Placing_BothFleetsComplete_StartsTheGameWithSeat1ToFire()
    {
        var (ann, bob) = await NewBattleAsync();

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();

        Assert.Equal(GamePhase.InProgress, annView.Phase);
        Assert.Equal(GamePhase.InProgress, bobView.Phase);
        Assert.True(annView.YourTurn);
        Assert.False(bobView.YourTurn);
    }

    [Fact]
    public async Task Placing_RandomForBothPlayers_StartsTheGame()
    {
        var (ann, bob) = await NewRoomAsync();

        (await ann.PlaceRandomAsync()).EnsureSuccessStatusCode();
        (await bob.PlaceRandomAsync()).EnsureSuccessStatusCode();

        Assert.Equal(GamePhase.InProgress, (await bob.ViewAsync()).Phase);
    }

    [Fact]
    public async Task Fire_PlayerWhoseTurnItIsNot_Returns409AndChangesNothing()
    {
        var (ann, bob) = await NewBattleAsync();
        var before = await bob.ViewAsync();

        var response = await bob.FireAsync(0, 0);

        await GameClient.AssertProblemAsync(response, 409);
        Assert.Equal(before.Version, (await bob.ViewAsync()).Version);
        Assert.True((await ann.ViewAsync()).YourTurn);
    }

    [Fact]
    public async Task Fire_ValidShot_PassesTheTurnToTheOtherPlayerAndAnswersWithoutBotShots()
    {
        var (ann, bob) = await NewBattleAsync();

        var shot = await ann.FireOkAsync(0, 0);

        Assert.Equal(ShotResult.Hit, shot.Result);
        Assert.Empty(shot.BotShots);
        Assert.False(shot.View.YourTurn);
        Assert.True((await bob.ViewAsync()).YourTurn);
    }

    [Fact]
    public async Task Fire_InvalidShot_KeepsTheTurn()
    {
        var (ann, _) = await NewBattleAsync();

        var shot = await ann.FireOkAsync(20, 20);

        Assert.Equal(ShotResult.Invalid, shot.Result);
        Assert.True(shot.View.YourTurn);
    }

    [Fact]
    public async Task Fire_ShowsTheShotOnTheVictimsOwnBoardAndOnTheShootersEnemyBoard()
    {
        var (ann, bob) = await NewBattleAsync();

        await ann.FireOkAsync(0, 0);
        await bob.FireOkAsync(9, 9);

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.Equal(OwnCell.Miss, annView.OwnBoard.Cells[9][9]);
        Assert.Equal(EnemyCell.Hit, annView.EnemyBoard.Cells[0][0]);
        Assert.Equal(OwnCell.Hit, bobView.OwnBoard.Cells[0][0]);
        Assert.Equal(EnemyCell.Miss, bobView.EnemyBoard.Cells[9][9]);
    }

    [Fact]
    public async Task Views_NeverContainTheOtherPlayersShipsOrToken()
    {
        var (ann, bob) = await NewBattleAsync();
        await ann.FireOkAsync(9, 9);
        await bob.FireOkAsync(9, 9);

        foreach (var (viewer, other) in new[] { (ann, bob), (bob, ann) })
        {
            using var document = JsonDocument.Parse(await (await viewer.GetAsync()).Content.ReadAsStringAsync());
            var json = document.RootElement.GetRawText();
            Assert.DoesNotContain(other.Token, json);
            var enemy = document.RootElement.GetProperty("enemyBoard").GetRawText();
            Assert.DoesNotContain("Ship", enemy);
        }
    }

    [Fact]
    public async Task Token_BelongsToOneSeat_SoAnnCannotFireAgainOnBobsTurn()
    {
        var (ann, _) = await NewBattleAsync();
        await ann.FireOkAsync(0, 0);

        var response = await ann.FireAsync(1, 0);

        await GameClient.AssertProblemAsync(response, 409);
    }

    [Fact]
    public async Task Token_FromAnotherRoom_Returns401()
    {
        var (ann, _) = await NewRoomAsync();
        var (other, _) = await NewRoomAsync();

        var response = await ann.GetAsync(other.Token);

        await GameClient.AssertProblemAsync(response, 401);
    }

    [Fact]
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
                    // Bob fires at the far corner away from the carrier rows, so he always has a fresh cell.
                    var bobShot = await bob.FireOkAsync(9 - (annShots - 1) % 10, 9 - (annShots - 1) / 10);
                    Assert.NotEqual(ShotResult.Invalid, bobShot.Result);
                }
            }
        }

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.Equal(GamePhase.Finished, annView.Phase);
        Assert.Equal("you", annView.Winner);
        Assert.Equal("opponent", bobView.Winner);
        Assert.Equal(0, annView.EnemyShipsRemaining);
        Assert.False(annView.YourTurn);
        Assert.False(bobView.YourTurn);
        Assert.Null(annView.EndReason);
        await GameClient.AssertProblemAsync(await bob.FireAsync(5, 5), 409);
    }
}
