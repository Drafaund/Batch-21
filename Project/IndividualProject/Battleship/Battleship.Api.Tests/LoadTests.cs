using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Battleship.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Battleship.Api.Tests;

// Reports latency and memory, so it must not share the CPU with other tests.
[NonParallelizable]
public class LoadTests : ApiTestFixture<ApiFactory>
{
    private static int FromEnvironment(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0 ? value : fallback;

    private static double Percentile(IReadOnlyList<double> sorted, double fraction)
        => sorted[Math.Max(0, (int)Math.Ceiling(fraction * sorted.Count) - 1)];

    private static async Task<T> TimedAsync<T>(ConcurrentBag<double> latencies, Func<Task<T>> action)
    {
        var started = Stopwatch.GetTimestamp();
        var result = await action();
        latencies.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return result;
    }

    private static async Task<int> PlayBotGameAsync(HttpClient http, string opponent, ConcurrentBag<double> latencies)
    {
        var game = await TimedAsync(latencies, () => GameClient.CreateAsync(http, "Ann", opponent));
        var placed = await TimedAsync(latencies, () => game.PlaceRandomAsync());
        placed.EnsureSuccessStatusCode();

        ShotResponse? last = null;
        var shots = 0;
        for (var y = 0; y < 10 && last?.View.Phase != GamePhase.Finished; y++)
        {
            for (var x = 0; x < 10 && last?.View.Phase != GamePhase.Finished; x++)
            {
                last = await TimedAsync(latencies, () => game.FireOkAsync(x, y));
                shots++;
                Assert.That(last.Result, Is.Not.EqualTo(ShotResult.Invalid));
            }
        }

        Assert.That(last!.View.Phase, Is.EqualTo(GamePhase.Finished));
        Assert.That(new[] { "you", "opponent" }, Does.Contain(last.View.Winner));
        return shots;
    }

    [Test]
    public async Task ManyBotGamesAtOnce_AllEndWithAWinnerAndTheRequestsStayFast()
    {
        var games = FromEnvironment("LOAD_GAMES", 100);
        var concurrency = FromEnvironment("LOAD_CONCURRENCY", 50);
        var store = Factory.Services.GetRequiredService<GameStore>();
        var latencies = new ConcurrentBag<double>();
        var gate = new SemaphoreSlim(concurrency);
        var totalShots = 0;

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
        var clock = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(0, games).Select(async index =>
        {
            await gate.WaitAsync();
            try
            {
                var shots = await PlayBotGameAsync(Http, index % 2 == 0 ? "Random" : "HuntTarget", latencies);
                Interlocked.Add(ref totalShots, shots);
            }
            finally
            {
                gate.Release();
            }
        }));
        clock.Stop();
        Factory.Logs.Clear();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        var sorted = latencies.OrderBy(value => value).ToList();
        var p95 = Percentile(sorted, 0.95);
        TestContext.Out.WriteLine($"{games} games ({concurrency} at a time), {sorted.Count} requests in {clock.Elapsed.TotalSeconds:F2} s = {sorted.Count / clock.Elapsed.TotalSeconds:F0} requests/s");
        TestContext.Out.WriteLine($"latency ms: p50 {Percentile(sorted, 0.50):F2}, p95 {p95:F2}, p99 {Percentile(sorted, 0.99):F2}, max {sorted[^1]:F2}");
        TestContext.Out.WriteLine($"shots fired: {totalShots}; games kept in the store: {store.Count}");
        TestContext.Out.WriteLine($"memory kept by {games} finished games: {(memoryAfter - memoryBefore) / 1024.0:F0} KB = {(memoryAfter - memoryBefore) / 1024.0 / games:F1} KB per game");

        Assert.That(store.Count >= games, Is.True);
        Assert.That(p95 < 1000, Is.True, $"p95 was {p95:F1} ms");
    }

    [Test]
    public async Task TwoPlayersFiringAtTheSameMoment_AreServedOneAtATime()
    {
        const int battles = 20;
        var rejected = 0;

        for (var round = 0; round < battles; round++)
        {
            var ann = await GameClient.CreateRoomAsync(Http, "Ann");
            var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");
            await ann.PlaceFleetAsync();
            await bob.PlaceFleetAsync();

            var results = await Task.WhenAll(KeepFiringAsync(ann), KeepFiringAsync(bob));
            Interlocked.Add(ref rejected, results[0].Rejected + results[1].Rejected);

            var difference = results[0].Accepted - results[1].Accepted;
            Assert.That(difference, Is.InRange(0, 1));

            Assert.That(CountShots(await ann.ViewAsync()), Is.EqualTo(results[0].Accepted));
            Assert.That(CountShots(await bob.ViewAsync()), Is.EqualTo(results[1].Accepted));
        }

        TestContext.Out.WriteLine($"{battles} battles with both players firing at once; {rejected} shots were refused because it was not their turn");
    }

    private static int CountShots(GameView view) => view.EnemyBoard.Cells.Sum(row => row.Count(cell => cell != EnemyCell.Unknown));

    private static async Task<(int Accepted, int Rejected)> KeepFiringAsync(GameClient player)
    {
        var accepted = 0;
        var rejected = 0;
        var next = 0;
        while (true)
        {
            var response = await player.FireAsync(next % 10, next / 10);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var shot = await GameClient.ReadAsync<ShotResponse>(response);
                accepted++;
                next++;
                if (shot.View.Phase == GamePhase.Finished)
                {
                    return (accepted, rejected);
                }
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                rejected++;
                if ((await player.ViewAsync()).Phase == GamePhase.Finished)
                {
                    return (accepted, rejected);
                }

                await Task.Delay(1);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected status {(int)response.StatusCode} while firing.");
            }
        }
    }

    [Test]
    public async Task ManyOnlineGamesAtOnce_EveryShotReachesBothPlayersOverSignalR()
    {
        var games = FromEnvironment("LOAD_ONLINE_GAMES", 20);
        const int shotsPerGame = 10;
        var clock = Stopwatch.StartNew();

        await Task.WhenAll(Enumerable.Range(0, games).Select(async _ =>
        {
            var ann = await GameClient.CreateRoomAsync(Http, "Ann");
            var bob = await GameClient.JoinAsync(Http, ann.Code, "Bob");
            await using var annHub = await HubClient.ConnectAsync(Factory, ann);
            await using var bobHub = await HubClient.ConnectAsync(Factory, bob);
            await ann.PlaceFleetAsync();
            await bob.PlaceFleetAsync();
            await annHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);
            await bobHub.WaitForAsync<GameStartedEvent>(GameEventNames.GameStarted);

            for (var shot = 0; shot < shotsPerGame; shot++)
            {
                var shooter = shot % 2 == 0 ? ann : bob;
                (await shooter.FireAsync(shot / 2, 0)).EnsureSuccessStatusCode();
            }

            foreach (var hub in new[] { annHub, bobHub })
            {
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (hub.All<ShotResolvedEvent>(GameEventNames.ShotResolved).Count() < shotsPerGame && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(10);
                }

                var events = hub.All<ShotResolvedEvent>(GameEventNames.ShotResolved).ToList();
                Assert.That(events.Count, Is.EqualTo(shotsPerGame));
                Assert.That(events.Select(e => e.View.Version).Distinct().Count(), Is.EqualTo(shotsPerGame));
                Assert.That(events.Count(e => e.Shooter == "you"), Is.EqualTo(shotsPerGame / 2));
            }
        }));
        clock.Stop();

        TestContext.Out.WriteLine($"{games} online games with 2 SignalR connections each: {games * shotsPerGame} shots, {games * shotsPerGame * 2} events delivered in {clock.Elapsed.TotalSeconds:F2} s");
    }
}
