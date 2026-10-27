using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;

namespace Battleship.Api.Tests;

internal sealed class HubClient : IAsyncDisposable
{
    private static readonly string[] EventNames =
    {
        GameEventNames.PlayerJoined,
        GameEventNames.GameStarted,
        GameEventNames.ShotResolved,
        GameEventNames.GameEnded,
        GameEventNames.OpponentConnection,
        GameEventNames.EmoteReceived,
    };

    private readonly object _sync = new();
    private readonly List<(string Name, JsonElement Payload)> _received = new();
    private readonly HubConnection _connection;

    private HubClient(HubConnection connection)
    {
        _connection = connection;
        foreach (var name in EventNames)
        {
            var captured = name;
            _connection.On<JsonElement>(captured, payload =>
            {
                lock (_sync)
                {
                    _received.Add((captured, payload));
                }
            });
        }
    }

    public IReadOnlyList<(string Name, JsonElement Payload)> Received
    {
        get
        {
            lock (_sync)
            {
                return _received.ToList();
            }
        }
    }

    public static async Task<HubClient> ConnectAsync(ApiFactory factory, GameClient player)
    {
        var client = Build(factory, player.Code, player.Token);
        await client._connection.StartAsync();
        return client;
    }

    public static HubClient Build(ApiFactory factory, string code, string? token)
    {
        var server = factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, $"{GameHub.Path}?code={code}"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
                options.Transports = HttpTransportType.WebSockets;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var uri = token == null ? context.Uri : new Uri($"{context.Uri}&access_token={Uri.EscapeDataString(token)}");
                    return await server.CreateWebSocketClient().ConnectAsync(uri, cancellationToken);
                };
            })
            .Build();
        return new HubClient(connection);
    }

    public Task StartAsync() => _connection.StartAsync();

    public Task StopAsync() => _connection.StopAsync();

    public async Task<T> WaitForAsync<T>(string name, Func<T, bool>? predicate = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            foreach (var (eventName, payload) in Received)
            {
                if (eventName != name)
                {
                    continue;
                }

                var typed = payload.Deserialize<T>(GameClient.Json)!;
                if (predicate == null || predicate(typed))
                {
                    return typed;
                }
            }

            await Task.Delay(20);
        }

        throw new TimeoutException($"No {name} event arrived. Received: {string.Join(", ", Received.Select(e => e.Name))}");
    }

    public IEnumerable<T> All<T>(string name)
    {
        return Received.Where(e => e.Name == name).Select(e => e.Payload.Deserialize<T>(GameClient.Json)!);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
