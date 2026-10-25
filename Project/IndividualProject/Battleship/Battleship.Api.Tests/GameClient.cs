using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Battleship.Core;

namespace Battleship.Api.Tests;

internal sealed class GameClient
{
    public const string TokenHeader = "X-Player-Token";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _http;

    private GameClient(HttpClient http, string code, string token, int seat, GameView initialView)
    {
        _http = http;
        Code = code;
        Token = token;
        Seat = seat;
        InitialView = initialView;
    }

    public string Code { get; }

    public string Token { get; }

    public int Seat { get; }

    public GameView InitialView { get; }

    public static async Task<GameClient> CreateAsync(HttpClient http, string name = "Ann", string opponent = "Random")
    {
        var response = await SendAsync(http, HttpMethod.Post, "/api/games", null, new { playerName = name, opponent });
        response.EnsureSuccessStatusCode();
        var created = await ReadAsync<CreateGameResponse>(response);
        return new GameClient(http, created.Code, created.PlayerToken, created.Seat, created.View);
    }

    public static Task<GameClient> CreateRoomAsync(HttpClient http, string name = "Ann") => CreateAsync(http, name, "Human");

    public static async Task<GameClient> JoinAsync(HttpClient http, string code, string name = "Bob")
    {
        var response = await JoinRawAsync(http, code, new { playerName = name });
        response.EnsureSuccessStatusCode();
        var joined = await ReadAsync<JoinGameResponse>(response);
        return new GameClient(http, code, joined.PlayerToken, joined.Seat, joined.View);
    }

    public static Task<HttpResponseMessage> JoinRawAsync(HttpClient http, string code, object? body)
        => SendAsync(http, HttpMethod.Post, $"/api/games/{code}/join", null, body);

    public static Task<HttpResponseMessage> SendAsync(HttpClient http, HttpMethod method, string url, string? token, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        if (token != null)
        {
            request.Headers.Add(TokenHeader, token);
        }

        if (body != null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        return http.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value!;
    }

    public static async Task AssertProblemAsync(HttpResponseMessage response, int expectedStatus)
    {
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("title").GetString()));
    }

    public Task<HttpResponseMessage> GetAsync() => GetAsync(Token);

    public Task<HttpResponseMessage> GetAsync(string? token) => SendAsync(_http, HttpMethod.Get, $"/api/games/{Code}", token, null);

    public async Task<GameView> ViewAsync() => await ReadAsync<GameView>(await GetAsync());

    public Task<HttpResponseMessage> PlaceShipAsync(ShipType type, int x, int y, Orientation orientation)
        => PlaceShipAsync(Token, type, x, y, orientation);

    public Task<HttpResponseMessage> PlaceShipAsync(string? token, ShipType type, int x, int y, Orientation orientation)
    {
        var body = new { type = type.ToString(), start = new { x, y }, orientation = orientation.ToString() };
        return SendAsync(_http, HttpMethod.Post, $"/api/games/{Code}/ships", token, body);
    }

    public Task<HttpResponseMessage> PostRawAsync(string path, object? body) => SendAsync(_http, HttpMethod.Post, $"/api/games/{Code}/{path}", Token, body);

    public Task<HttpResponseMessage> EmoteAsync(string? emote) => EmoteAsync(Token, emote);

    public Task<HttpResponseMessage> EmoteAsync(string? token, string? emote)
        => SendAsync(_http, HttpMethod.Post, $"/api/games/{Code}/emotes", token, new { emote });

    public Task<HttpResponseMessage> PlaceRandomAsync() => PlaceRandomAsync(Token);

    public Task<HttpResponseMessage> PlaceRandomAsync(string? token)
        => SendAsync(_http, HttpMethod.Post, $"/api/games/{Code}/ships/random", token, null);

    public Task<HttpResponseMessage> FireAsync(int x, int y) => FireAsync(Token, x, y);

    public Task<HttpResponseMessage> FireAsync(string? token, int x, int y)
        => SendAsync(_http, HttpMethod.Post, $"/api/games/{Code}/shots", token, new { target = new { x, y } });

    public async Task<ShotResponse> FireOkAsync(int x, int y)
    {
        var response = await FireAsync(x, y);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<ShotResponse>(response);
    }

    public async Task<GameView> PlaceFleetAsync()
    {
        GameView? view = null;
        foreach (var type in Enum.GetValues<ShipType>())
        {
            var response = await PlaceShipAsync(type, 0, (int)type, Orientation.Horizontal);
            response.EnsureSuccessStatusCode();
            view = await ReadAsync<GameView>(response);
        }

        return view!;
    }
}
