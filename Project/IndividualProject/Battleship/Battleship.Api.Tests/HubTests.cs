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

public class HubTests : ApiTestFixture<FastForfeitApiFactory>
{
    private async Task<(GameClient Ann, GameClient Bob)> NewRoomAsync()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");
        var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");
        return (ann, bob);
    }

    private async Task<(GameClient Ann, GameClient Bob, HubClient AnnHub, HubClient BobHub)> NewConnectedBattleAsync()
    {
        var (ann, bob) = await NewRoomAsync();
        var annHub = await HubClient.ConnectAsync(Factory, ann);
        var bobHub = await HubClient.ConnectAsync(Factory, bob);
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        await annHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
        await bobHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
        return (ann, bob, annHub, bobHub);
    }

    [Test]
    public async Task Connect_WrongToken_IsRefusedWith401()
    {
        var ann = await GameClient.CreateRoomAsync(Http);
        await using var hub = HubClient.Build(Factory, ann.Code, "wrong-token");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Connect_MissingToken_IsRefusedWith401()
    {
        var ann = await GameClient.CreateRoomAsync(Http);
        await using var hub = HubClient.Build(Factory, ann.Code, null);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Connect_UnknownCode_IsRefusedWith404()
    {
        await using var hub = HubClient.Build(Factory, "ZZZZZ", "any-token");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Connect_TokenOfAnotherRoom_IsRefused()
    {
        var ann = await GameClient.CreateRoomAsync(Http);
        var other = await GameClient.CreateRoomAsync(Http);
        await using var hub = HubClient.Build(Factory, ann.Code, other.Token);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => hub.StartAsync());

        Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Join_TellsTheWaitingPlayerWhoArrived()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");
        await using var annHub = await HubClient.ConnectAsync(Factory, ann);

        var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");

        var joined = await annHub.WaitForAsync<PlayerJoinedEvent>(GameEventNames.PlayerJoined);
        Assert.That(joined.Name, Is.EqualTo("Bob"));
        Assert.That(joined.View.Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(joined.View.Opponent?.Name, Is.EqualTo("Bob"));
        Assert.That(bob.Seat, Is.EqualTo(2));
    }

    [Test]
    public async Task Connect_SecondPlayerConnecting_TellsTheFirstThatTheOpponentIsHere()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(Factory, ann);

        await using var bobHub = await HubClient.ConnectAsync(Factory, bob);

        var connection = await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected);
        Assert.That(connection.View.Opponent!.Connected, Is.True);
        Assert.That((await ann.ViewAsync()).Opponent!.Connected, Is.True);
    }

    [Test]
    public async Task Disconnect_TellsTheOpponentAndReconnectingTellsThemAgain()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(Factory, ann);
        var bobHub = await HubClient.ConnectAsync(Factory, bob);
        await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected);

        await bobHub.StopAsync();

        var dropped = await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => !e.Connected);
        Assert.That(dropped.View.Opponent!.Connected, Is.False);
        Assert.That((await ann.ViewAsync()).Opponent!.Connected, Is.False);

        await using var returned = await HubClient.ConnectAsync(Factory, bob);
        var back = await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected && e.View.Version > dropped.View.Version);
        Assert.That(back.View.Opponent!.Connected, Is.True);
    }

    [Test]
    public async Task Connect_SecondTabOfTheSamePlayer_DoesNotCountAsADrop()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(Factory, ann);
        await using var firstTab = await HubClient.ConnectAsync(Factory, bob);
        await using var secondTab = await HubClient.ConnectAsync(Factory, bob);
        await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => e.Connected);

        await firstTab.StopAsync();
        await Task.Delay(200);

        Assert.That(annHub.All<OpponentConnectionEvent>(GameEventNames.OpponentConnection).Any(e => !e.Connected), Is.False);
        Assert.That((await ann.ViewAsync()).Opponent!.Connected, Is.True);
    }

    [Test]
    public async Task Placing_BothFleetsComplete_SendsGameStartedToBothWithTheirOwnView()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        var annStarted = await annHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
        var bobStarted = await bobHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);

        Assert.That(annStarted.View.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(annStarted.View.You.Seat, Is.EqualTo(1));
        Assert.That(annStarted.View.YourTurn, Is.True);
        Assert.That(bobStarted.View.You.Seat, Is.EqualTo(2));
        Assert.That(bobStarted.View.YourTurn, Is.False);
        Assert.That(bobStarted.View.Opponent?.Name, Is.EqualTo("Ann"));
        Assert.That(bob.Code, Is.EqualTo(ann.Code));
    }

    [Test]
    public async Task Fire_SendsShotResolvedToBothPlayersFromTheirOwnPointOfView()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        var response = await ann.FireOkAsync(0, 0);

        var forAnn = await annHub.WaitForAsync<ShotResolvedEvent>(GameEventNames.ShotResolved);
        var forBob = await bobHub.WaitForAsync<ShotResolvedEvent>(GameEventNames.ShotResolved);
        Assert.That(forAnn.Shooter, Is.EqualTo("you"));
        Assert.That(forBob.Shooter, Is.EqualTo("opponent"));
        Assert.That(forBob.Target, Is.EqualTo(new CoordinateDto(0, 0)));
        Assert.That(forBob.Result, Is.EqualTo(ShotResult.Hit));
        Assert.That(forAnn.Result, Is.EqualTo(ShotResult.Hit));
        Assert.That(forAnn.View.YourTurn, Is.False);
        Assert.That(forBob.View.YourTurn, Is.True);
        Assert.That(forBob.View.OwnBoard.Cells[0][0], Is.EqualTo(OwnCell.Hit));
        Assert.That(forAnn.View.EnemyBoard.Cells[0][0], Is.EqualTo(EnemyCell.Hit));
        Assert.That(forAnn.View.Version, Is.EqualTo(response.View.Version));
        Assert.That(forBob.View.Version, Is.EqualTo(forAnn.View.Version));
        Assert.That(forBob.View.You.Seat, Is.EqualTo(bob.Seat));
    }

    [Test]
    public async Task Fire_InvalidShot_SendsNoEvent()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await ann.FireOkAsync(50, 50);
        await Task.Delay(200);

        Assert.That(bobHub.All<ShotResolvedEvent>(GameEventNames.ShotResolved), Is.Empty);
        Assert.That(annHub.All<ShotResolvedEvent>(GameEventNames.ShotResolved), Is.Empty);
    }

    [Test]
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
            Assert.That(hub.Received, Is.Not.Empty);
            foreach (var (_, payload) in hub.Received)
            {
                var json = payload.GetRawText();
                Assert.That(json, Does.Not.Contain(other.Token));
                Assert.That(json, Does.Not.Contain("playerToken").IgnoreCase);
                Assert.That(payload.GetProperty("view").GetProperty("enemyBoard").GetRawText(), Does.Not.Contain("Ship\""));
            }
        }
    }

    [Test]
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
        Assert.That(forAnn.Winner, Is.EqualTo("you"));
        Assert.That(forBob.Winner, Is.EqualTo("opponent"));
        Assert.That(forBob.View.Phase, Is.EqualTo(GamePhase.Finished));
        Assert.That(forBob.View.Winner, Is.EqualTo("opponent"));
        Assert.That(forAnn.View.EndReason, Is.Null);
        Assert.That(annHub.All<GameEndedEvent>(GameEventNames.GameEnded), Has.Exactly(1).Items);
    }

    [Test]
    public async Task Disconnect_DuringABattleForLongerThanTheTimeout_GivesTheOtherPlayerTheWin()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();

        var ended = await annHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);

        Assert.That(ended.Winner, Is.EqualTo("you"));
        Assert.That(ended.View.EndReason, Is.EqualTo("Forfeit"));
        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();
        Assert.That(annView.Phase, Is.EqualTo(GamePhase.Finished));
        Assert.That(annView.Winner, Is.EqualTo("you"));
        Assert.That(bobView.Winner, Is.EqualTo("opponent"));
        Assert.That(bobView.EndReason, Is.EqualTo("Forfeit"));
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
    }

    [Test]
    public async Task Disconnect_ForfeitLogLine_DoesNotCarryTheRequestThatStartedTheTimer()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();
        await annHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);

        var entries = Factory.Logs.ForGame(ann.Code);
        var forfeit = entries.Single(entry => entry.EventName == LogEvents.PlayerForfeited);

        Assert.That(forfeit.Properties.Keys, Does.Not.Contain("RequestId"));
        Assert.That(forfeit.Properties.Keys, Does.Not.Contain("RequestPath"));
        Assert.That(forfeit.TraceId, Is.Null);
        var joined = entries.First(entry => entry.EventName == LogEvents.PlayerJoined);
        Assert.That(joined.Properties.Keys, Does.Contain("RequestPath"));
        Assert.That(joined.TraceId, Is.Not.Null);
    }

    [Test]
    public async Task Disconnect_ReturningBeforeTheTimeout_KeepsTheGameGoing()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();
        await annHub.WaitForAsync<OpponentConnectionEvent>(GameEventNames.OpponentConnection, e => !e.Connected);

        await using var returned = await HubClient.ConnectAsync(Factory, bob);
        await Task.Delay(700);

        Assert.That(annHub.All<GameEndedEvent>(GameEventNames.GameEnded), Is.Empty);
        var view = await ann.ViewAsync();
        Assert.That(view.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(view.Opponent!.Connected, Is.True);
    }

    [Test]
    public async Task Disconnect_WhileStillPlacingShips_DoesNotForfeit()
    {
        var (ann, bob) = await NewRoomAsync();
        await using var annHub = await HubClient.ConnectAsync(Factory, ann);
        var bobHub = await HubClient.ConnectAsync(Factory, bob);
        await bobHub.StopAsync();

        await Task.Delay(700);

        Assert.That((await ann.ViewAsync()).Phase, Is.EqualTo(GamePhase.Placing));
        Assert.That(annHub.All<GameEndedEvent>(GameEventNames.GameEnded), Is.Empty);
    }

    [Test]
    public async Task Reconnect_AfterMissingShots_TheViewFromRestHoldsEverythingTheClientMissed()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedBattleAsync();
        await using var _ = annHub;
        await bobHub.StopAsync();
        await ann.FireOkAsync(0, 0);

        var view = await bob.ViewAsync();

        Assert.That(view.OwnBoard.Cells[0][0], Is.EqualTo(OwnCell.Hit));
        Assert.That(view.YourTurn, Is.True);
    }
}
