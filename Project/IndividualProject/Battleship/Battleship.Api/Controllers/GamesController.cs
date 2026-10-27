using Microsoft.AspNetCore.Mvc;

namespace Battleship.Api;

[ApiController]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private const string TokenHeader = "X-Player-Token";

    private readonly GameStore _store;

    public GamesController(GameStore store)
    {
        _store = store;
    }

    [HttpPost]
    public ActionResult<CreateGameResponse> CreateGame(CreateGameRequest? request)
    {
        var name = PlayerNameValidator.Validate(request?.PlayerName);
        if (name.IsFailure)
        {
            return name.Error.ToActionResult(HttpContext);
        }

        if (request?.Opponent is not { } opponent)
        {
            return Error.Validation("opponent is required: Random, HuntTarget, or Human.").ToActionResult(HttpContext);
        }

        var session = _store.Create(name.Value, opponent);
        var response = new CreateGameResponse(session.Code, session.PlayerToken, Seat: 1, session.GetView(1));
        return Created($"/api/games/{session.Code}", response);
    }

    [HttpPost("{code}/join")]
    public ActionResult<JoinGameResponse> JoinGame(string code, JoinGameRequest? request)
    {
        return PlayerNameValidator.Validate(request?.PlayerName)
            .Bind(name => _store.Join(code, name))
            .ToActionResult(HttpContext);
    }

    [HttpGet("{code}")]
    public ActionResult<GameView> GetGame(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        return _store.GetAuthorized(code, token)
            .Map(access => access.Session.GetView(access.Seat))
            .ToActionResult(HttpContext);
    }

    [HttpPost("{code}/ships")]
    public ActionResult<GameView> PlaceShip(string code, PlaceShipRequest? request, [FromHeader(Name = TokenHeader)] string? token)
    {
        return _store.GetAuthorized(code, token)
            .Bind(access =>
            {
                if (request?.Type is not { } type || request.Start is not { } start || request.Orientation is not { } orientation)
                {
                    return Error.Validation("type, start, and orientation are required.");
                }

                return access.Session.PlaceShip(access.Seat, type, start.ToCoordinate(), orientation);
            })
            .ToActionResult(HttpContext);
    }

    [HttpPost("{code}/ships/random")]
    public ActionResult<GameView> PlaceShipsRandomly(string code, [FromHeader(Name = TokenHeader)] string? token)
    {
        return _store.GetAuthorized(code, token)
            .Bind(access => access.Session.PlaceShipsRandomly(access.Seat))
            .ToActionResult(HttpContext);
    }

    [HttpPost("{code}/shots")]
    public ActionResult<ShotResponse> Fire(string code, FireRequest? request, [FromHeader(Name = TokenHeader)] string? token)
    {
        return _store.GetAuthorized(code, token)
            .Bind(access =>
            {
                if (request?.Target is not { } target)
                {
                    return Error.Validation("target is required.");
                }

                return access.Session.Fire(access.Seat, target.ToCoordinate());
            })
            .ToActionResult(HttpContext);
    }

    [HttpPost("{code}/emotes")]
    public ActionResult SendEmote(string code, EmoteRequest? request, [FromHeader(Name = TokenHeader)] string? token)
    {
        var access = _store.GetAuthorized(code, token);
        if (access.IsFailure)
        {
            return access.Error.ToActionResult(HttpContext);
        }

        if (!EmoteCatalog.IsKnown(request?.Emote))
        {
            return Error.Validation($"emote must be one of: {string.Join(", ", EmoteCatalog.Codes)}.").ToActionResult(HttpContext);
        }

        return access.Value.Session.SendEmote(access.Value.Seat, request!.Emote!).ToActionResult(HttpContext);
    }
}
