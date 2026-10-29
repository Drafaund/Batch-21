using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battleship.Api.Tests;

public sealed class FastTurnApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<GameOptions>();
            services.AddSingleton(new GameOptions { TurnTimeout = TimeSpan.FromMilliseconds(700) });
        });
    }
}

public class TurnTimeoutTests
{
    private FastTurnApiFactory _fast = null!;
    private ApiFactory _normalOptions = null!;
    private HttpClient _fastHttp = null!;
    private HttpClient _normalHttp = null!;

    [OneTimeSetUp]
    public void CreateFactories()
    {
        _fast = new FastTurnApiFactory();
        _normalOptions = new ApiFactory();
    }

    [OneTimeTearDown]
    public async Task DisposeFactories()
    {
        await _fast.DisposeAsync();
        await _normalOptions.DisposeAsync();
    }

    [SetUp]
    public void CreateClients()
    {
        _fastHttp = _fast.CreateClient();
        _normalHttp = _normalOptions.CreateClient();
    }

    private async Task<(GameClient Ann, GameClient Bob)> NewBattleAsync(HttpClient http)
    {
        var ann = await GameClient.CreateRoomAsync(http, "Ann");
        var bob = await GameClient.JoinAsync(http, ann.Code, "Bob");
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        return (ann, bob);
    }

    private static async Task<GameView> WaitUntilFinishedAsync(GameClient player)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var view = await player.ViewAsync();
            if (view.Phase == GamePhase.Finished)
            {
                return view;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The game did not finish.");
    }

    [Test]
    public async Task Battle_PlayerToFireDoesNothing_LosesAndTheOtherPlayerWins()
    {
        var (ann, bob) = await NewBattleAsync(_fastHttp);

        var annView = await WaitUntilFinishedAsync(ann);
        var bobView = await bob.ViewAsync();

        Assert.That(annView.Winner, Is.EqualTo("opponent"));
        Assert.That(annView.EndReason, Is.EqualTo("Timeout"));
        Assert.That(bobView.Winner, Is.EqualTo("you"));
        Assert.That(bobView.EndReason, Is.EqualTo("Timeout"));
        Assert.That(annView.YourTurn, Is.False);
        Assert.That(annView.TurnSecondsRemaining, Is.Null);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
    }

    [Test]
    public async Task Battle_TimeoutLogLine_DoesNotCarryTheRequestThatStartedTheClock()
    {
        var (ann, _) = await NewBattleAsync(_fastHttp);
        await WaitUntilFinishedAsync(ann);

        var entries = _fast.Logs.ForGame(ann.Code);
        var timeout = entries.Single(entry => entry.EventName == LogEvents.TurnTimedOut);

        Assert.That(timeout.Properties.Keys, Does.Not.Contain("RequestId"));
        Assert.That(timeout.Properties.Keys, Does.Not.Contain("RequestPath"));
        Assert.That(timeout.TraceId, Is.Null);
        var joined = entries.First(entry => entry.EventName == LogEvents.PlayerJoined);
        Assert.That(joined.Properties.Keys, Does.Contain("RequestPath"));
        Assert.That(joined.TraceId, Is.Not.Null);
    }

    [Test]
    public async Task Battle_TimeoutIsSentToBothPlayersWithTheirOwnVerdict()
    {
        var ann = await GameClient.CreateRoomAsync(_fastHttp, "Ann");
        var bob = await GameClient.JoinAsync(_fastHttp, ann.Code, "Bob");
        await using var annHub = await HubClient.ConnectAsync(_fast, ann);
        await using var bobHub = await HubClient.ConnectAsync(_fast, bob);
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();

        var forAnn = await annHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);
        var forBob = await bobHub.WaitForAsync<GameEndedEvent>(GameEventNames.GameEnded);

        Assert.That(forAnn.Winner, Is.EqualTo("opponent"));
        Assert.That(forBob.Winner, Is.EqualTo("you"));
        Assert.That(forAnn.View.EndReason, Is.EqualTo("Timeout"));
        Assert.That(forBob.View.EndReason, Is.EqualTo("Timeout"));
    }

    [Test]
    public async Task Battle_ValidShotPassesTheClockToTheOtherPlayer()
    {
        var (ann, bob) = await NewBattleAsync(_fastHttp);

        await ann.FireOkAsync(0, 0);
        var finished = await WaitUntilFinishedAsync(bob);

        Assert.That(finished.Winner, Is.EqualTo("opponent"));
        Assert.That(finished.EndReason, Is.EqualTo("Timeout"));
        Assert.That((await ann.ViewAsync()).Winner, Is.EqualTo("you"));
    }

    [Test]
    public async Task Battle_PlayersFiringInTimeKeepPlayingPastTheTimeout()
    {
        var (ann, bob) = await NewBattleAsync(_fastHttp);

        for (var i = 0; i < 4; i++)
        {
            await ann.FireOkAsync(i, 0);
            await Task.Delay(250);
            await bob.FireOkAsync(9 - i, 9);
            await Task.Delay(250);
        }

        var view = await ann.ViewAsync();
        Assert.That(view.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(view.EndReason, Is.Null);
    }

    [Test]
    public async Task Battle_InvalidShotDoesNotGiveTheTurnBackItsFullTime()
    {
        var (ann, _) = await NewBattleAsync(_fastHttp);

        await Task.Delay(400);
        await ann.FireOkAsync(50, 50);
        var finished = await WaitUntilFinishedAsync(ann);

        Assert.That(finished.EndReason, Is.EqualTo("Timeout"));
        Assert.That(finished.Winner, Is.EqualTo("opponent"));
    }

    [Test]
    public async Task Timeouts_CanBeSetInConfiguration()
    {
        await using var factory = new ApiFactory().WithWebHostBuilder(builder => builder.UseSetting("Game:TurnTimeout", "00:00:00.700"));
        var (ann, _) = await NewBattleAsync(factory.CreateClient());

        var finished = await WaitUntilFinishedAsync(ann);

        Assert.That(finished.EndReason, Is.EqualTo("Timeout"));
    }

    [Test]
    public void Timeouts_DefaultToThirtySecondsInAppsettings()
    {
        var options = _normalOptions.Services.GetRequiredService<GameOptions>();

        Assert.That(options.TurnTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(options.OpponentTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public async Task Battle_ViewTellsHowManySecondsTheMoverHasLeft()
    {
        var (ann, bob) = await NewBattleAsync(_normalHttp);

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();

        Assert.That(annView.TurnSecondsRemaining!.Value, Is.InRange(1, 30));
        Assert.That(bobView.TurnSecondsRemaining!.Value, Is.InRange(1, 30));
    }

    [Test]
    public async Task NoBattleYet_ViewHasNoClock()
    {
        var ann = await GameClient.CreateRoomAsync(_normalHttp, "Ann");
        Assert.That(ann.InitialView.TurnSecondsRemaining, Is.Null);

        var bob = await GameClient.JoinAsync(_normalHttp, ann.Code, "Bob");
        await ann.PlaceFleetAsync();

        Assert.That(bob.InitialView.TurnSecondsRemaining, Is.Null);
        Assert.That((await ann.ViewAsync()).TurnSecondsRemaining, Is.Null);
    }

    [Test]
    public async Task GameAgainstABot_HasNoTurnClockAndNeverTimesOut()
    {
        var game = await GameClient.CreateAsync(_fastHttp, opponent: "Random");
        await game.PlaceFleetAsync();

        await Task.Delay(1200);

        var view = await game.ViewAsync();
        Assert.That(view.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(view.TurnSecondsRemaining, Is.Null);
        Assert.That(view.EndReason, Is.Null);
    }

    [Test]
    public async Task Battle_FinishedByTheLastShot_LeavesNoClockRunning()
    {
        var (ann, bob) = await NewBattleAsync(_normalHttp);

        for (var i = 0; i < 100; i++)
        {
            var shot = await ann.FireOkAsync(i % 10, i / 10);
            if (shot.View.Phase == GamePhase.Finished)
            {
                break;
            }

            await bob.FireOkAsync(9 - i % 10, 9 - i / 10);
        }

        var view = await bob.ViewAsync();
        Assert.That(view.Phase, Is.EqualTo(GamePhase.Finished));
        Assert.That(view.EndReason, Is.Null);
        Assert.That(view.TurnSecondsRemaining, Is.Null);
    }
}
