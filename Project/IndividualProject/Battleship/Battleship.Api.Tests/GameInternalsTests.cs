using Battleship.Core;
using Serilog.Core;

namespace Battleship.Api.Tests;

public class GameInternalsTests
{
    private static GameSessionFactory NewFactory()
    {
        return new GameSessionFactory(() => new Random(1), Logger.None, new RecordingSink(), new GameOptions());
    }

    [Test]
    public void OnlinePlayer_SelectTarget_Throws()
    {
        var player = new OnlinePlayer(1, "Ann", new Board(), new Fleet());

        Assert.Throws<InvalidOperationException>(() => player.SelectTarget());
    }

    [Test]
    public void OnlinePlayer_ReceiveResult_DoesNothing()
    {
        var player = new OnlinePlayer(1, "Ann", new Board(), new Fleet());

        Assert.DoesNotThrow(() => player.ReceiveResult(new Coordinate(0, 0), ShotResult.Hit));
    }

    [Test]
    public void OnlinePlayer_IsAPlayerWithIdentityBoardAndFleet()
    {
        var board = new Board();
        var fleet = new Fleet();

        var player = new OnlinePlayer(1, "Ann", board, fleet);

        Assert.That(player, Is.AssignableTo<IPlayer>());
        Assert.That(player, Is.AssignableTo<IShotStrategy>());
        Assert.That(player.Name, Is.EqualTo("Ann"));
        Assert.That(player.Board, Is.SameAs(board));
        Assert.That(player.Fleet, Is.SameAs(fleet));
    }

    [Test]
    public void GameSessionFactory_HumanOpponent_StartsInTheLobbyWithNothingPlaced()
    {
        var factory = NewFactory();

        var session = factory.Create("ABCDE", "token", "Ann", OpponentKind.Human);

        var view = session.GetView(1);
        Assert.That(view.Phase, Is.EqualTo(GamePhase.Lobby));
        Assert.That(view.Opponent, Is.Null);
        Assert.That(view.OwnShips, Is.Empty);
    }

    [Test]
    public void GameSessionFactory_BotOpponent_PlacesTheBotFleetSoOnlyThePlayerHasToPlace()
    {
        var factory = NewFactory();

        var session = factory.Create("ABCDE", "token", "Ann", OpponentKind.HuntTarget);

        var view = session.GetView(1);
        Assert.That(view.EnemyShipsRemaining, Is.EqualTo(5));
        Assert.That(view.OwnShips, Is.Empty);
    }

    [Test]
    public void GameSession_GameAgainstABot_SendsNoEvents()
    {
        var sink = new RecordingSink();
        var factory = new GameSessionFactory(() => new Random(1), Logger.None, sink, new GameOptions());
        var session = factory.Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        session.PlaceShipsRandomly(1);
        session.Fire(1, new Coordinate(0, 0));
        session.Connect(1);
        session.Disconnect(1);

        Assert.That(sink.Events, Is.Empty);
    }

    [Test]
    public void GameSession_FindSeat_OnlyAcceptsTheExactToken()
    {
        var factory = NewFactory();
        var session = factory.Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        Assert.That(session.FindSeat("secret"), Is.EqualTo(1));
        Assert.That(session.FindSeat("Secret"), Is.Null);
        Assert.That(session.FindSeat("secre"), Is.Null);
        Assert.That(session.FindSeat("secret "), Is.Null);
        Assert.That(session.FindSeat(""), Is.Null);
        Assert.That(session.FindSeat(null), Is.Null);
    }

    [Test]
    public void GameStore_CreateAndFind_ReturnsTheSameSessionForAnyLetterCase()
    {
        var store = new GameStore(NewFactory());

        var session = store.Create("Ann", OpponentKind.Random);

        Assert.That(store.Find(session.Code.ToLowerInvariant()).Value, Is.SameAs(session));
        Assert.That(store.Count, Is.EqualTo(1));
    }

    [Test]
    public void GameStore_Find_UnknownCode_FailsWithNotFound()
    {
        var store = new GameStore(NewFactory());

        var result = store.Find("ZZZZZ");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.NotFound));
    }

    [Test]
    public void GameStore_GetAuthorized_WrongToken_FailsWithUnauthorized()
    {
        var store = new GameStore(NewFactory());
        var session = store.Create("Ann", OpponentKind.Random);

        var result = store.GetAuthorized(session.Code, "wrong");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Unauthorized));
    }

    [Test]
    public void GameStore_GetAuthorized_RightToken_ReturnsTheSessionAndSeat()
    {
        var store = new GameStore(NewFactory());
        var session = store.Create("Ann", OpponentKind.Random);

        var result = store.GetAuthorized(session.Code, session.PlayerToken);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Session, Is.SameAs(session));
        Assert.That(result.Value.Seat, Is.EqualTo(1));
    }

    [Test]
    public void GameSession_Fire_BeforeShipsArePlaced_FailsWithConflict()
    {
        var session = NewFactory().Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        var result = session.Fire(1, new Coordinate(0, 0));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Conflict));
    }

    [Test]
    public void GameSession_Join_BotGame_FailsWithConflict()
    {
        var session = NewFactory().Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        var result = session.Join("Bob", "other");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Conflict));
    }

    [Test]
    public void GameSession_SendEmote_BotGame_FailsWithConflict()
    {
        var session = NewFactory().Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        var result = session.SendEmote(1, "gg");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Conflict));
    }

    [Test]
    public void GameStore_Create_ManyGamesInParallel_AllGetDifferentCodes()
    {
        var store = new GameStore(NewFactory());

        var sessions = Enumerable.Range(0, 200).AsParallel().Select(_ => store.Create("Ann", OpponentKind.Random)).ToList();

        Assert.That(sessions.Select(s => s.Code).Distinct().Count(), Is.EqualTo(200));
        Assert.That(store.Count, Is.EqualTo(200));
    }

    [TestCase("Ann")]
    [TestCase(" Ann ")]
    [TestCase("a")]
    public void PlayerNameValidator_GoodNames_AreTrimmedAndAccepted(string name)
    {
        Assert.That(PlayerNameValidator.Validate(name).Value, Is.EqualTo(name.Trim()));
    }

    [TestCase(null)]
    [TestCase("   ")]
    [TestCase("a name that is far too long")]
    [TestCase("bad\nname")]
    public void PlayerNameValidator_BadNames_FailWithValidation(string? name)
    {
        var result = PlayerNameValidator.Validate(name);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Validation));
    }
}
