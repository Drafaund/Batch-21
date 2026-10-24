using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battleship.Api.Tests;

/// <summary>Same as <see cref="ApiFactory"/>, but a seat may send an emote again after a fraction of a second.</summary>
public sealed class FastEmoteApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<GameOptions>();
            services.AddSingleton(new GameOptions { EmoteCooldown = TimeSpan.FromMilliseconds(300) });
        });
    }
}

/// <summary>Emotes between two humans: who may send one, what the other player is told, and the limits.</summary>
public class EmoteTests : IClassFixture<FastEmoteApiFactory>
{
    private readonly FastEmoteApiFactory _factory;
    private readonly HttpClient _http;

    public EmoteTests(FastEmoteApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    private async Task<(GameClient Ann, GameClient Bob, HubClient AnnHub, HubClient BobHub)> NewConnectedRoomAsync()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");
        var bob = await GameClient.JoinAsync(_http, ann.Code, "Bob");
        var annHub = await HubClient.ConnectAsync(_factory, ann);
        var bobHub = await HubClient.ConnectAsync(_factory, bob);
        return (ann, bob, annHub, bobHub);
    }

    [Fact]
    public async Task Send_KnownEmote_TellsOnlyTheOpponent()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        var response = await ann.EmoteAsync("thumbsUp");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var received = await bobHub.WaitForAsync<EmoteReceivedEvent>(GameEventNames.EmoteReceived);
        Assert.Equal("thumbsUp", received.Emote);
        await Task.Delay(200);
        Assert.Empty(annHub.All<EmoteReceivedEvent>(GameEventNames.EmoteReceived));
    }

    [Fact]
    public async Task Send_TheOtherWayAround_ReachesTheFirstPlayer()
    {
        var (_, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await bob.EmoteAsync("laugh");

        var received = await annHub.WaitForAsync<EmoteReceivedEvent>(GameEventNames.EmoteReceived);
        Assert.Equal("laugh", received.Emote);
    }

    [Fact]
    public async Task Send_DuringABattle_IsAllowedAndDoesNotChangeTheGame()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        var before = await ann.ViewAsync();

        var response = await bob.EmoteAsync("wow");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await ann.ViewAsync();
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.YourTurn, after.YourTurn);
    }

    [Fact]
    public async Task Send_UnknownEmote_Gives400()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("<script>alert(1)</script>"), 400);
        await GameClient.AssertProblemAsync(await ann.EmoteAsync(""), 400);
        await GameClient.AssertProblemAsync(await ann.EmoteAsync(null), 400);
    }

    [Fact]
    public async Task Send_WithoutABody_Gives400()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await GameClient.AssertProblemAsync(await ann.PostRawAsync("emotes", null), 400);
    }

    [Fact]
    public async Task Send_WithoutOrWithAWrongToken_Gives401()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await GameClient.AssertProblemAsync(await ann.EmoteAsync(null, "thumbsUp"), 401);
        await GameClient.AssertProblemAsync(await ann.EmoteAsync("wrong-token", "thumbsUp"), 401);
    }

    [Fact]
    public async Task Send_AgainstABot_Gives409()
    {
        var ann = await GameClient.CreateAsync(_http, "Ann", "Random");

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("thumbsUp"), 409);
    }

    [Fact]
    public async Task Send_BeforeTheOpponentHasJoined_Gives409()
    {
        var ann = await GameClient.CreateRoomAsync(_http, "Ann");

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("thumbsUp"), 409);
    }

    [Fact]
    public async Task Send_UnknownGame_Gives404()
    {
        var response = await GameClient.SendAsync(_http, HttpMethod.Post, "/api/games/ZZZZZ/emotes", "any-token", new { emote = "thumbsUp" });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Fact]
    public async Task Send_TwiceInARow_Gives429ThenWorksAfterTheCooldown()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        Assert.Equal(HttpStatusCode.NoContent, (await ann.EmoteAsync("clap")).StatusCode);

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("clap"), 429);
        await Task.Delay(400);

        Assert.Equal(HttpStatusCode.NoContent, (await ann.EmoteAsync("clap")).StatusCode);
        await Task.Delay(200);
        Assert.Equal(2, bobHub.All<EmoteReceivedEvent>(GameEventNames.EmoteReceived).Count());
    }

    [Fact]
    public async Task Send_TheCooldownBelongsToTheSeat_SoTheOpponentCanAnswerAtOnce()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        await ann.EmoteAsync("wave");

        var response = await bob.EmoteAsync("wave");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Send_RefusedEmote_ReachesNobody()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await ann.EmoteAsync("not-an-emote");
        await Task.Delay(200);

        Assert.Empty(bobHub.All<EmoteReceivedEvent>(GameEventNames.EmoteReceived));
    }

    [Fact]
    public async Task Event_CarriesOnlyTheEmote_NoViewAndNoToken()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await ann.EmoteAsync("target");
        await bobHub.WaitForAsync<EmoteReceivedEvent>(GameEventNames.EmoteReceived);

        var payload = bobHub.Received.Single(e => e.Name == GameEventNames.EmoteReceived).Payload;
        var json = payload.GetRawText();
        Assert.DoesNotContain(ann.Token, json);
        Assert.DoesNotContain(bob.Token, json);
        Assert.False(payload.TryGetProperty("view", out var view), $"unexpected view: {view}");
        Assert.Single(payload.EnumerateObject());
    }

    [Fact]
    public void EmoteCatalog_HoldsTheCodesTheFrontEndKnows()
    {
        string[] expected = { "thumbsUp", "laugh", "wow", "angry", "target", "wave", "handshake", "clap" };

        Assert.Equal(expected.OrderBy(code => code), EmoteCatalog.Codes.OrderBy(code => code));
        Assert.True(EmoteCatalog.IsKnown("clap"));
        Assert.False(EmoteCatalog.IsKnown("Clap"));
        Assert.False(EmoteCatalog.IsKnown(null));
    }
}
