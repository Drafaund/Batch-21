using Battleship.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Battleship.Api.Tests;

public class GameInternalsTests
{
    private static GameSessionFactory NewFactory()
    {
        return new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance, new RecordingSink(), new GameOptions());
    }

    [Fact]
    public void OnlinePlayer_SelectTarget_Throws()
    {
        var player = new OnlinePlayer(1, "Ann", new Board(), new Fleet());

        Assert.Throws<InvalidOperationException>(() => player.SelectTarget());
    }

    [Fact]
    public void OnlinePlayer_ReceiveResult_DoesNothing()
    {
        var player = new OnlinePlayer(1, "Ann", new Board(), new Fleet());

        var exception = Record.Exception(() => player.ReceiveResult(new Coordinate(0, 0), ShotResult.Hit));

        Assert.Null(exception);
    }

    [Fact]
    public void OnlinePlayer_IsAPlayerWithIdentityBoardAndFleet()
    {
        var board = new Board();
        var fleet = new Fleet();

        var player = new OnlinePlayer(1, "Ann", board, fleet);

        Assert.IsAssignableFrom<IPlayer>(player);
        Assert.IsAssignableFrom<IShotStrategy>(player);
        Assert.Equal("Ann", player.Name);
        Assert.Same(board, player.Board);
        Assert.Same(fleet, player.Fleet);
    }

    [Fact]
    public void GameSessionFactory_HumanOpponent_StartsInTheLobbyWithNothingPlaced()
    {
        var factory = NewFactory();

        var session = factory.Create("ABCDE", "token", "Ann", OpponentKind.Human);

        var view = session.GetView(1);
        Assert.Equal(GamePhase.Lobby, view.Phase);
        Assert.Null(view.Opponent);
        Assert.Empty(view.OwnShips);
    }

    [Fact]
    public void GameSessionFactory_BotOpponent_PlacesTheBotFleetSoOnlyThePlayerHasToPlace()
    {
        var factory = NewFactory();

        var session = factory.Create("ABCDE", "token", "Ann", OpponentKind.HuntTarget);

        var view = session.GetView(1);
        Assert.Equal(5, view.EnemyShipsRemaining);
        Assert.Empty(view.OwnShips);
    }

    [Fact]
    public void GameSession_GameAgainstABot_SendsNoEvents()
    {
        var sink = new RecordingSink();
        var factory = new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance, sink, new GameOptions());
        var session = factory.Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        session.PlaceShipsRandomly(1);
        session.Fire(1, new Coordinate(0, 0));
        session.Connect(1);
        session.Disconnect(1);

        Assert.Empty(sink.Events);
    }

    [Fact]
    public void GameSession_FindSeat_OnlyAcceptsTheExactToken()
    {
        var factory = NewFactory();
        var session = factory.Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        Assert.Equal(1, session.FindSeat("secret"));
        Assert.Null(session.FindSeat("Secret"));
        Assert.Null(session.FindSeat("secre"));
        Assert.Null(session.FindSeat("secret "));
        Assert.Null(session.FindSeat(""));
        Assert.Null(session.FindSeat(null));
    }

    [Fact]
    public void GameStore_CreateAndFind_ReturnsTheSameSessionForAnyLetterCase()
    {
        var store = new GameStore(NewFactory());

        var session = store.Create("Ann", OpponentKind.Random);

        Assert.Same(session, store.Find(session.Code.ToLowerInvariant()).Value);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void GameStore_Find_UnknownCode_FailsWithNotFound()
    {
        var store = new GameStore(NewFactory());

        var result = store.Find("ZZZZZ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void GameStore_GetAuthorized_WrongToken_FailsWithUnauthorized()
    {
        var store = new GameStore(NewFactory());
        var session = store.Create("Ann", OpponentKind.Random);

        var result = store.GetAuthorized(session.Code, "wrong");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public void GameStore_GetAuthorized_RightToken_ReturnsTheSessionAndSeat()
    {
        var store = new GameStore(NewFactory());
        var session = store.Create("Ann", OpponentKind.Random);

        var result = store.GetAuthorized(session.Code, session.PlayerToken);

        Assert.True(result.IsSuccess);
        Assert.Same(session, result.Value.Session);
        Assert.Equal(1, result.Value.Seat);
    }

    [Fact]
    public void GameSession_Fire_BeforeShipsArePlaced_FailsWithConflict()
    {
        var session = NewFactory().Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        var result = session.Fire(1, new Coordinate(0, 0));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void GameSession_Join_BotGame_FailsWithConflict()
    {
        var session = NewFactory().Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        var result = session.Join("Bob", "other");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void GameSession_SendEmote_BotGame_FailsWithConflict()
    {
        var session = NewFactory().Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        var result = session.SendEmote(1, "gg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void GameStore_Create_ManyGamesInParallel_AllGetDifferentCodes()
    {
        var store = new GameStore(NewFactory());

        var sessions = Enumerable.Range(0, 200).AsParallel().Select(_ => store.Create("Ann", OpponentKind.Random)).ToList();

        Assert.Equal(200, sessions.Select(s => s.Code).Distinct().Count());
        Assert.Equal(200, store.Count);
    }

    [Theory]
    [InlineData("Ann")]
    [InlineData(" Ann ")]
    [InlineData("a")]
    public void PlayerNameValidator_GoodNames_AreTrimmedAndAccepted(string name)
    {
        Assert.Equal(name.Trim(), PlayerNameValidator.Validate(name).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("a name that is far too long")]
    [InlineData("bad\nname")]
    public void PlayerNameValidator_BadNames_FailWithValidation(string? name)
    {
        var result = PlayerNameValidator.Validate(name);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
