using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battleship.Api.Tests;

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

public class EmoteTests : ApiTestFixture<FastEmoteApiFactory>
{
    private async Task<(GameClient Ann, GameClient Bob, HubClient AnnHub, HubClient BobHub)> NewConnectedRoomAsync()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");
        var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");
        var annHub = await HubClient.ConnectAsync(Factory, ann);
        var bobHub = await HubClient.ConnectAsync(Factory, bob);
        return (ann, bob, annHub, bobHub);
    }

    [Test]
    public async Task Send_KnownEmote_TellsOnlyTheOpponent()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        var response = await ann.EmoteAsync("thumbsUp");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var received = await bobHub.WaitForAsync<EmoteReceivedEvent>(GameEventNames.EmoteReceived);
        Assert.That(received.Emote, Is.EqualTo("thumbsUp"));
        await Task.Delay(200);
        Assert.That(annHub.All<EmoteReceivedEvent>(GameEventNames.EmoteReceived), Is.Empty);
    }

    [Test]
    public async Task Send_TheOtherWayAround_ReachesTheFirstPlayer()
    {
        var (_, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await bob.EmoteAsync("laugh");

        var received = await annHub.WaitForAsync<EmoteReceivedEvent>(GameEventNames.EmoteReceived);
        Assert.That(received.Emote, Is.EqualTo("laugh"));
    }

    [Test]
    public async Task Send_DuringABattle_IsAllowedAndDoesNotChangeTheGame()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        await ann.PlaceFleetAsync();
        await bob.PlaceFleetAsync();
        var before = await ann.ViewAsync();

        var response = await bob.EmoteAsync("wow");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var after = await ann.ViewAsync();
        Assert.That(after.Version, Is.EqualTo(before.Version));
        Assert.That(after.YourTurn, Is.EqualTo(before.YourTurn));
    }

    [Test]
    public async Task Send_UnknownEmote_Gives400()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("<script>alert(1)</script>"), 400);
        await GameClient.AssertProblemAsync(await ann.EmoteAsync(""), 400);
        await GameClient.AssertProblemAsync(await ann.EmoteAsync(null), 400);
    }

    [Test]
    public async Task Send_WithoutABody_Gives400()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await GameClient.AssertProblemAsync(await ann.PostRawAsync("emotes", null), 400);
    }

    [Test]
    public async Task Send_WithoutOrWithAWrongToken_Gives401()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await GameClient.AssertProblemAsync(await ann.EmoteAsync(null, "thumbsUp"), 401);
        await GameClient.AssertProblemAsync(await ann.EmoteAsync("wrong-token", "thumbsUp"), 401);
    }

    [Test]
    public async Task Send_AgainstABot_Gives409()
    {
        var ann = await GameClient.CreateAsync(Http, "Ann", "Random");

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("thumbsUp"), 409);
    }

    [Test]
    public async Task Send_BeforeTheOpponentHasJoined_Gives409()
    {
        var ann = await GameClient.CreateRoomAsync(Http, "Ann");

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("thumbsUp"), 409);
    }

    [Test]
    public async Task Send_UnknownGame_Gives404()
    {
        var response = await GameClient.SendAsync(Http, HttpMethod.Post, "/api/games/ZZZZZ/emotes", "any-token", new { emote = "thumbsUp" });

        await GameClient.AssertProblemAsync(response, 404);
    }

    [Test]
    public async Task Send_TwiceInARow_Gives429ThenWorksAfterTheCooldown()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        Assert.That((await ann.EmoteAsync("clap")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await GameClient.AssertProblemAsync(await ann.EmoteAsync("clap"), 429);
        await Task.Delay(400);

        Assert.That((await ann.EmoteAsync("clap")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await Task.Delay(200);
        Assert.That(bobHub.All<EmoteReceivedEvent>(GameEventNames.EmoteReceived).Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Send_TheCooldownBelongsToTheSeat_SoTheOpponentCanAnswerAtOnce()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;
        await ann.EmoteAsync("wave");

        var response = await bob.EmoteAsync("wave");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Send_RefusedEmote_ReachesNobody()
    {
        var (ann, _, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await ann.EmoteAsync("not-an-emote");
        await Task.Delay(200);

        Assert.That(bobHub.All<EmoteReceivedEvent>(GameEventNames.EmoteReceived), Is.Empty);
    }

    [Test]
    public async Task Event_CarriesOnlyTheEmote_NoViewAndNoToken()
    {
        var (ann, bob, annHub, bobHub) = await NewConnectedRoomAsync();
        await using var _ = annHub;
        await using var __ = bobHub;

        await ann.EmoteAsync("target");
        await bobHub.WaitForAsync<EmoteReceivedEvent>(GameEventNames.EmoteReceived);

        var payload = bobHub.Received.Single(e => e.Name == GameEventNames.EmoteReceived).Payload;
        var json = payload.GetRawText();
        Assert.That(json, Does.Not.Contain(ann.Token));
        Assert.That(json, Does.Not.Contain(bob.Token));
        Assert.That(payload.TryGetProperty("view", out var view), Is.False, $"unexpected view: {view}");
        Assert.That(payload.EnumerateObject(), Has.Exactly(1).Items);
    }

    [Test]
    public void EmoteCatalog_HoldsTheCodesTheFrontEndKnows()
    {
        string[] expected = { "thumbsUp", "laugh", "wow", "angry", "target", "wave", "handshake", "clap" };

        Assert.That(EmoteCatalog.Codes.OrderBy(code => code), Is.EqualTo(expected.OrderBy(code => code)));
        Assert.That(EmoteCatalog.IsKnown("clap"), Is.True);
        Assert.That(EmoteCatalog.IsKnown("Clap"), Is.False);
        Assert.That(EmoteCatalog.IsKnown(null), Is.False);
    }
}
