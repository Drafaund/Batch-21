using System.Net;
using Battleship.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battleship.Api.Tests;

public sealed class FastForfeitApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<GameOptions>();
            services.AddSingleton(new GameOptions { OpponentTimeout = TimeSpan.FromMilliseconds(300) });
        });
    }
}

public class HubTests : IClassFixture<FastForfeitApiFactory>
{
    private readonly FastForfeitApiFactory _factory;
    private readonly HttpClient _http;

    public HubTests(FastForfeitApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    private async Task<(GameClient Ann, GameClient Bob)> NewRoomAsync()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");
        var bob = await GameClient.JoinAsync(_http, ann.Code, "Bob");
        return (ann, bob);
    }

    private async Task<(GameClient Ann, GameClient Bob, HubClient AnnHub, HubClient BobHub)> NewConnectedBattleAsync()
    {
        var (ann, bob) = await NewRoomAsync();
        var annHub = await HubClient.ConnectAsync(_factory, ann);
        var bobHub = await HubClient.ConnectAsync(_factory, bob);
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        await annHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
        await bobHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
        return (ann, bob, annHub, bobHub);
    }

    [Fact]
    public async Task Connect_WrongToken_IsRefusedWith401()
    {
        var ann = await GameClient.CreateRoomAsync(_http);
        await using var hub = HubClient.Build(_factory, ann.Code, "wrong-token");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Connect_MissingToken_IsRefusedWith401()
    {
        var ann = await GameClient.CreateRoomAsync(_http);
        await using var hub = HubClient.Build(_factory, ann.Code, null);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Connect_UnknownCode_IsRefusedWith404()
    {
        await using var hub = HubClient.Build(_factory, "ZZZZZ", "any-token");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task Connect_TokenOfAnotherRoom_IsRefused()
    {
        var ann = await GameClient.CreateRoomAsync(_http);
        var other = await GameClient.CreateRoomAsync(_http);
        await using var hub = HubClient.Build(_factory, ann.Code, other.Token);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Join_TellsTheWaitingPlayerWhoArrived()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");
        await using var annHub = await HubClient.ConnectAsync(_factory, ann);

        var bob = await GameClient.JoinAsync(_http, ann.Code, "Bob");

        var joined = await annHub.WaitForAsync<PlayerJoinedEvent>(GameEventNames.PlayerJoined);
        Assert.Equal("Bob", joined.Name);
        Assert.Equal(GamePhase.Placing, joined.View.Phase);
        Assert.Equal("Bob", joined.View.Opponent?.Name);
        Assert.Equal(2, bob.Seat);
    }

    [Fact]
    public async Task Connect_SecondPlayerConnecting_TellsTheFirstThatTheOpponentIsHere()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(_factory, ann);

        await using var bobHub = await HubClient.ConnectAsync(_factory, bob);

        var connection = await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected);
        Assert.True(connection.View.Opponent!.Connected);
        Assert.True((await ann.ViewAsync()).Opponent!.Connected);
    }

    [Fact]
    public async Task Disconnect_TellsTheOpponentAndReconnectingTellsThemAgain()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(_factory, ann);
        var bobHub = await HubClient.ConnectAsync(_factory, bob);
        await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected);

        await bobHub.StopAsync();

        var dropped = await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => !e.Connected);
        Assert.False(dropped.View.Opponent!.Connected);
        Assert.False((await ann.ViewAsync()).Opponent!.Connected);

        await using var returned = await HubClient.ConnectAsync(_factory, bob);
        var back = await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected && e.View.Version > dropped.View.Version);
        Assert.True(back.View.Opponent!.Connected);
    }

    [Fact]
    public async Task Connect_SecondTabOfTheSamePlayer_DoesNotCountAsADrop()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(_factory, ann);
        await using var firstTab = await HubClient.ConnectAsync(_factory, bob);
        await using var secondTab = await HubClient.ConnectAsync(_factory, bob);
        await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected);

        await firstTab.StopAsync();
        await Task.Delay(200);

        Assert.DoesNotContain(annHub.All<OpponentConnectionEvent>(GameEventNames.OpponentConnection), e => !e.Connected);
        Assert.True((await ann.ViewAsync()).Opponent!.Connected);
    }

    [Fact]
    public async Task Placing_BothFleetsComplete_SendsGameStartedToBothWithTheirOwnView()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        var annStarted = await annHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
        var bobStarted = await bobHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);

        Assert.Equal(GamePhase.InProgress, annStarted.View.Phase);
        Assert.Equal(1, annStarted.View.You.Seat);
        Assert.True(annStarted.View.YourTurn);
        Assert.Equal(2, bobStarted.View.You.Seat);
        Assert.False(bobStarted.View.YourTurn);
        Assert.Equal("Ann", bobStarted.View.Opponent?.Name);
        Assert.Equal(ann.Code, bob.Code);
    }

    [Fact]
    public async Task Fire_SendsShotResolvedToBothPlayersFromTheirOwnPointOfView()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        var response = await ann.FireOkAsync(0, 0);

        var forAnn = await annHub.WaitForAsync<ShotResolvedEvent>(GameEventNames.ShotResolved);
        var forBob = await bobHub.WaitForAsync<ShotResolvedEvent>(GameEventNames.ShotResolved);
        Assert.Equal("you", forAnn.Shooter);
        Assert.Equal("opponent", forBob.Shooter);
        Assert.Equal(new CoordinateDto(0, 0), forBob.Target);
        Assert.Equal(ShotResult.Hit, forBob.Result);
        Assert.Equal(ShotResult.Hit, forAnn.Result);
        Assert.False(forAnn.View.YourTurn);
        Assert.True(forBob.View.YourTurn);
        Assert.Equal(OwnCell.Hit, forBob.View.OwnBoard.Cells[0][0]);
        Assert.Equal(EnemyCell.Hit, forAnn.View.EnemyBoard.Cells[0][0]);
        Assert.Equal(response.View.Version, forAnn.View.Version);
        Assert.Equal(forAnn.View.Version, forBob.View.Version);
        Assert.Equal(bob.Seat, forBob.View.You.Seat);
    }

    [Fact]
    public async Task Fire_InvalidShot_SendsNoEvent()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await ann.FireOkAsync(50, 50);
        await Task.Delay(200);

        Assert.Empty(bobHub.All<ShotResolvedEvent>(GameEventNames.ShotResolved));
        Assert.Empty(annHub.All<ShotResolvedEvent>(GameEventNames.ShotResolved));
    }

    [Fact]
    public async Task Events_NeverCarryATokenOrTheOpponentsShips()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        await ann.FireOkAsync(9, 9);
        await bob.FireOkAsync(9, 9);
        await annHub.WaitForAsync<ShotResolvedEvent>(GameEventNames.ShotResolved, e => e.Shooter == "opponent");

        foreach (var (hub, other) in new[] { (annHub, bob), (bobHub, ann) })
        {
            Assert.NotEmpty(hub.Received);
            foreach (var (_, payload) in hub.Received)
            {
                var json = payload.GetRawText();
                Assert.DoesNotContain(other.Token, json);
                Assert.DoesNotContain("playerToken", json, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Ship\"", payload.GetProperty("view").GetProperty("enemyBoard").GetRawText());
            }
        }
    }

    [Fact]
    public async Task FullGame_SendsGameEndedToBothAndEachGetsTheirOwnVerdict()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        for (var i = 0; i < 100; i++)
        {
            var shot = await ann.FireOkAsync(i % 10, i / 10);
            if (shot.View.Phase == GamePhase.Finished)
            {
                break;
            }

            await bob.FireOkAsync(9 - i % 10, 9 - i / 10);
        }

        var forAnn = await annHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);
        var forBob = await bobHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);
        Assert.Equal("you", forAnn.Winner);
        Assert.Equal("opponent", forBob.Winner);
        Assert.Equal(GamePhase.Finished, forBob.View.Phase);
        Assert.Equal("opponent", forBob.View.Winner);
        Assert.Null(forAnn.View.EndReason);
        Assert.Single(annHub.All<GameEndedEvent>(GameEventNames.GameEnded));
    }

    [Fact]
    public async Task Disconnect_DuringABattleForLongerThanTheTimeout_GivesTheOtherPlayerTheWin()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();

        var ended = await annHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);

        Assert.Equal("you", ended.Winner);
        Assert.Equal("Forfeit", ended.View.EndReason);
        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.Equal(GamePhase.Finished, annView.Phase);
        Assert.Equal("you", annView.Winner);
        Assert.Equal("opponent", bobView.Winner);
        Assert.Equal("Forfeit", bobView.EndReason);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
    }

    [Fact]
    public async Task Disconnect_ReturningBeforeTheTimeout_KeepsTheGameGoing()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();
        await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => !e.Connected);

        await using var returned = await HubClient.ConnectAsync(_factory, bob);
        await Task.Delay(700);

        Assert.Empty(annHub.All<GameEndedEvent>(GameEventNames.GameEnded));
        var view = await ann.ViewAsync();
        Assert.Equal(GamePhase.InProgress, view.Phase);
        Assert.True(view.Opponent!.Connected);
    }

    [Fact]
    public async Task Disconnect_WhileStillPlacingShips_DoesNotForfeit()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(_factory, ann);
        var bobHub = await HubClient.ConnectAsync(_factory, bob);
        await bobHub.StopAsync();

        await Task.Delay(700);

        Assert.Equal(GamePhase.Placing, (await ann.ViewAsync()).Phase);
        Assert.Empty(annHub.All<GameEndedEvent>(GameEventNames.GameEnded));
    }

    [Fact]
    public async Task Reconnect_AfterMissingShots_TheViewFromRestHoldsEverythingTheClientMissed()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();
        await ann.FireOkAsync(0, 0);

        var view = await bob.ViewAsync();

        Assert.Equal(OwnCell.Hit, view.OwnBoard.Cells[0][0]);
        Assert.True(view.YourTurn);
    }
}
