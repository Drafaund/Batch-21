using Microsoft.AspNetCore.Mvc;

namespace Battleship.Api;

/// <summary>The REST endpoints of a game. They check the token and the input, then call the session.</summary>
internal static class GameEndpoints
{
    private const string TokenHeader = "X-Player-Token";

    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games").WithTags("Games");
        games.MapPost("/", CreateGame);
        games.MapPost("/{code}/join", JoinGame);
        games.MapGet("/{code}", GetGame);
        games.MapPost("/{code}/ships", PlaceShip);
        games.MapPost("/{code}/ships/random", PlaceShipsRandomly);
        games.MapPost("/{code}/shots", Fire);
        games.MapPost("/{code}/emotes", SendEmote);
        return app;
    }

    private static IResult CreateGame(CreateGameRequest? request, GameStore store)
    {
        var name = PlayerNameValidator.Validate(request?.PlayerName);
        if (request?.Opponent is not { } opponent)
        {
            throw ApiException.BadRequest("opponent is required: Random, HuntTarget, or Human.");
        }

        var session = store.Create(name, opponent);
        var response = new CreateGameResponse(session.Code, session.PlayerToken, Seat: 1, session.GetView(1));
        return Results.Created($"/api/games/{session.Code}", response);
    }

    private static JoinGameResponse JoinGame(string code, JoinGameRequest? request, GameStore store)
    {
        return store.Join(code, PlayerNameValidator.Validate(request?.PlayerName));
    }

    private static GameView GetGame(string code, [FromHeader(Name = TokenHeader)] string? token, GameStore store)
    {
        var access = store.GetAuthorized(code, token);
        return access.Session.GetView(access.Seat);
    }

    private static GameView PlaceShip(
        string code,
        PlaceShipRequest? request,
        [FromHeader(Name = TokenHeader)] string? token,
        GameStore store)
    {
        var access = store.GetAuthorized(code, token);
        if (request?.Type is not { } type || request.Start is not { } start || request.Orientation is not { } orientation)
        {
            throw ApiException.BadRequest("type, start, and orientation are required.");
        }

        return access.Session.PlaceShip(access.Seat, type, start.ToCoordinate(), orientation);
    }

    private static GameView PlaceShipsRandomly(string code, [FromHeader(Name = TokenHeader)] string? token, GameStore store)
    {
        var access = store.GetAuthorized(code, token);
        return access.Session.PlaceShipsRandomly(access.Seat);
    }

    private static ShotResponse Fire(
        string code,
        FireRequest? request,
        [FromHeader(Name = TokenHeader)] string? token,
        GameStore store)
    {
        var access = store.GetAuthorized(code, token);
        if (request?.Target is not { } target)
        {
            throw ApiException.BadRequest("target is required.");
        }

        return access.Session.Fire(access.Seat, target.ToCoordinate());
    }

    private static IResult SendEmote(
        string code,
        EmoteRequest? request,
        [FromHeader(Name = TokenHeader)] string? token,
        GameStore store)
    {
        var access = store.GetAuthorized(code, token);
        if (!EmoteCatalog.IsKnown(request?.Emote))
        {
            throw ApiException.BadRequest($"emote must be one of: {string.Join(", ", EmoteCatalog.Codes)}.");
        }

        access.Session.SendEmote(access.Seat, request!.Emote!);
        return Results.NoContent();
    }
}
