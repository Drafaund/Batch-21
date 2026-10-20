using Battleship.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Battleship.Api.Tests;

/// <summary>Small checks of the classes behind the endpoints, without HTTP.</summary>
public class GameInternalsTests
{
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
    public void GameSessionFactory_HumanOpponent_IsNotSupportedYet()
    {
        var factory = new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance);

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create("ABCDE", "token", "Ann", OpponentKind.Human));
    }

    [Fact]
    public void GameSessionFactory_BotOpponent_PlacesTheBotFleetSoOnlyThePlayerHasToPlace()
    {
        var factory = new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance);

        var session = factory.Create("ABCDE", "token", "Ann", OpponentKind.HuntTarget);

        var view = session.GetView();
        Assert.Equal(5, view.EnemyShipsRemaining);
        Assert.Empty(view.OwnShips);
    }

    [Fact]
    public void GameSession_IsAuthorized_OnlyAcceptsTheExactToken()
    {
        var factory = new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance);
        var session = factory.Create("ABCDE", "secret", "Ann", OpponentKind.Random);

        Assert.True(session.IsAuthorized("secret"));
        Assert.False(session.IsAuthorized("Secret"));
        Assert.False(session.IsAuthorized("secre"));
        Assert.False(session.IsAuthorized("secret "));
        Assert.False(session.IsAuthorized(""));
        Assert.False(session.IsAuthorized(null));
    }

    [Fact]
    public void GameStore_CreateAndFind_ReturnsTheSameSessionForAnyLetterCase()
    {
        var store = new GameStore(new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance));

        var session = store.Create("Ann", OpponentKind.Random);

        Assert.Same(session, store.Find(session.Code.ToLowerInvariant()));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void GameStore_Create_ManyGamesInParallel_AllGetDifferentCodes()
    {
        var store = new GameStore(new GameSessionFactory(() => new Random(1), NullLoggerFactory.Instance));

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
        Assert.Equal(name.Trim(), PlayerNameValidator.Validate(name));
    }

    [Fact]
    public void PlayerNameValidator_Null_ThrowsBadRequest()
    {
        var exception = Assert.Throws<ApiException>(() => PlayerNameValidator.Validate(null));

        Assert.Equal(400, exception.StatusCode);
    }
}
