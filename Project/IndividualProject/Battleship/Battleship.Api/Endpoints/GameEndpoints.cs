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
        games.MapGet("/{code}", GetGame);
        games.MapPost("/{code}/ships", PlaceShip);
        games.MapPost("/{code}/ships/random", PlaceShipsRandomly);
        games.MapPost("/{code}/shots", Fire);
        return app;
    }

    private static IResult CreateGame(CreateGameRequest? request, GameStore store)
    {
        var name = PlayerNameValidator.Validate(request?.PlayerName);
        if (request?.Opponent is not { } opponent)
        {
            throw ApiException.BadRequest("opponent is required: Random or HuntTarget.");
        }

        if (opponent == OpponentKind.Human)
        {
            throw ApiException.BadRequest("Games against another human are not available yet. Use Random or HuntTarget.");
        }

        var session = store.Create(name, opponent);
        var response = new CreateGameResponse(session.Code, session.PlayerToken, Seat: 1, session.GetView());
        return Results.Created($"/api/games/{session.Code}", response);
    }

    private static GameView GetGame(string code, [FromHeader(Name = TokenHeader)] string? token, GameStore store)
    {
        return store.GetAuthorized(code, token).GetView();
    }

    private static GameView PlaceShip(
        string code,
        PlaceShipRequest? request,
        [FromHeader(Name = TokenHeader)] string? token,
        GameStore store)
    {
        var session = store.GetAuthorized(code, token);
        if (request?.Type is not { } type || request.Start is not { } start || request.Orientation is not { } orientation)
        {
            throw ApiException.BadRequest("type, start, and orientation are required.");
        }

        return session.PlaceShip(type, start.ToCoordinate(), orientation);
    }

    private static GameView PlaceShipsRandomly(string code, [FromHeader(Name = TokenHeader)] string? token, GameStore store)
    {
        return store.GetAuthorized(code, token).PlaceShipsRandomly();
    }

    private static ShotResponse Fire(
        string code,
        FireRequest? request,
        [FromHeader(Name = TokenHeader)] string? token,
        GameStore store)
    {
        var session = store.GetAuthorized(code, token);
        if (request?.Target is not { } target)
        {
            throw ApiException.BadRequest("target is required.");
        }

        return session.Fire(target.ToCoordinate());
    }
}
