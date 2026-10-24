using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battleship.Api.Tests;

/// <summary>Same as <see cref="ApiFactory"/>, but the player to fire loses after a fraction of a second.</summary>
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

/// <summary>The player whose turn it is must fire in time, or the other player wins.</summary>
public class TurnTimeoutTests : IClassFixture<FastTurnApiFactory>, IClassFixture<ApiFactory>
{
    private readonly FastTurnApiFactory _fast;
    private readonly HttpClient _fastHttp;
    private readonly HttpClient _normalHttp;
    private readonly ApiFactory _normalOptions;

    public TurnTimeoutTests(FastTurnApiFactory fast, ApiFactory normal)
    {
        _fast = fast;
        _fastHttp = fast.CreateClient();
        _normalHttp = normal.CreateClient();
        _normalOptions = normal;
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

    [Fact]
    public async Task Battle_PlayerToFireDoesNothing_LosesAndTheOtherPlayerWins()
    {
        var (ann, bob) = await NewBattleAsync(_fastHttp);

        var annView = await WaitUntilFinishedAsync(ann);
        var bobView = await bob.ViewAsync();

        Assert.Equal("opponent", annView.Winner);
        Assert.Equal("Timeout", annView.EndReason);
        Assert.Equal("you", bobView.Winner);
        Assert.Equal("Timeout", bobView.EndReason);
        Assert.False(annView.YourTurn);
        Assert.Null(annView.TurnSecondsRemaining);
        await GameClient.AssertProblemAsync(await ann.FireAsync(0, 0), 409);
    }

    [Fact]
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

        Assert.Equal("opponent", forAnn.Winner);
        Assert.Equal("you", forBob.Winner);
        Assert.Equal("Timeout", forAnn.View.EndReason);
        Assert.Equal("Timeout", forBob.View.EndReason);
    }

    [Fact]
    public async Task Battle_ValidShotPassesTheClockToTheOtherPlayer()
    {
        var (ann, bob) = await NewBattleAsync(_fastHttp);

        await ann.FireOkAsync(0, 0);
        var finished = await WaitUntilFinishedAsync(bob);

        // Ann fired in time, so it is Bob who ran out of time.
        Assert.Equal("opponent", finished.Winner);
        Assert.Equal("Timeout", finished.EndReason);
        Assert.Equal("you", (await ann.ViewAsync()).Winner);
    }

    [Fact]
    public async Task Battle_PlayersFiringInTimeKeepPlayingPastTheTimeout()
    {
        var (ann, bob) = await NewBattleAsync(_fastHttp);

        // Eight shots, 250 ms apart: two seconds in total, far more than the 700 ms one player may take.
        for (var i = 0; i < 4; i++)
        {
            await ann.FireOkAsync(i, 0);
            await Task.Delay(250);
            await bob.FireOkAsync(9 - i, 9);
            await Task.Delay(250);
        }

        var view = await ann.ViewAsync();
        Assert.Equal(GamePhase.InProgress, view.Phase);
        Assert.Null(view.EndReason);
    }

    [Fact]
    public async Task Battle_InvalidShotDoesNotGiveTheTurnBackItsFullTime()
    {
        var (ann, _) = await NewBattleAsync(_fastHttp);

        await Task.Delay(400);
        await ann.FireOkAsync(50, 50);
        var finished = await WaitUntilFinishedAsync(ann);

        // The invalid shot kept the turn and the clock that was already running.
        Assert.Equal("Timeout", finished.EndReason);
        Assert.Equal("opponent", finished.Winner);
    }

    [Fact]
    public async Task Timeouts_CanBeSetInConfiguration()
    {
        await using var factory = new ApiFactory().WithWebHostBuilder(builder => builder.UseSetting("Game:TurnTimeout", "00:00:00.700"));
        var (ann, _) = await NewBattleAsync(factory.CreateClient());

        var finished = await WaitUntilFinishedAsync(ann);

        Assert.Equal("Timeout", finished.EndReason);
    }

    [Fact]
    public void Timeouts_DefaultToThirtySecondsInAppsettings()
    {
        var options = _normalOptions.Services.GetRequiredService<GameOptions>();

        Assert.Equal(TimeSpan.FromSeconds(30), options.TurnTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), options.OpponentTimeout);
    }

    [Fact]
    public async Task Battle_ViewTellsHowManySecondsTheMoverHasLeft()
    {
        var (ann, bob) = await NewBattleAsync(_normalHttp);

        var annView = await ann.ViewAsync();
        var bobView = await bob.ViewAsync();

        Assert.InRange(annView.TurnSecondsRemaining!.Value, 1, 30);
        Assert.InRange(bobView.TurnSecondsRemaining!.Value, 1, 30);
    }

    [Fact]
    public async Task NoBattleYet_ViewHasNoClock()
    {
        var ann = await GameClient.CreateRoomAsync(_normalHttp, "Ann");
        Assert.Null(ann.InitialView.TurnSecondsRemaining);

        var bob = await GameClient.JoinAsync(_normalHttp, ann.Code, "Bob");
        await ann.PlaceFleetAsync();

        Assert.Null(bob.InitialView.TurnSecondsRemaining);
        Assert.Null((await ann.ViewAsync()).TurnSecondsRemaining);
    }

    [Fact]
    public async Task GameAgainstABot_HasNoTurnClockAndNeverTimesOut()
    {
        var game = await GameClient.CreateAsync(_fastHttp, opponent: "Random");
        await game.PlaceFleetAsync();

        await Task.Delay(1200);

        var view = await game.ViewAsync();
        Assert.Equal(GamePhase.InProgress, view.Phase);
        Assert.Null(view.TurnSecondsRemaining);
        Assert.Null(view.EndReason);
    }

    [Fact]
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
        Assert.Equal(GamePhase.Finished, view.Phase);
        Assert.Null(view.EndReason);
        Assert.Null(view.TurnSecondsRemaining);
    }
}
