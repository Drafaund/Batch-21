namespace Othello.Core.Tests;

public class AutoPlayerTests
{
    [Fact]
    public void GreedyAIPlayer_ChooseMove_PicksTheCandidateWithMostFlips()
    {
        var moveOne = new Move(new Position(2, 3), PieceColor.Black, new[]
        {
            new Position(3, 3),
            new Position(4, 3)
        });
        var moveTwo = new Move(new Position(2, 4), PieceColor.Black, new[]
        {
            new Position(3, 4)
        });

        var player = new GreedyAIPlayer(PieceColor.Black, "Greedy");

        var chosen = player.ChooseMove(new[] { moveOne, moveTwo });

        Assert.Equal(moveOne, chosen);
    }

    [Fact]
    public void RandomAIPlayer_ChooseMove_WithFixedSeed_IsDeterministicAndIsInCandidates()
    {
        var candidates = new IReadOnlyList<Move>[]
        {
            new[]
            {
                new Move(new Position(2, 3), PieceColor.Black, new[] { new Position(3, 3) }),
                new Move(new Position(3, 2), PieceColor.Black, new[] { new Position(3, 3) }),
                new Move(new Position(4, 5), PieceColor.Black, new[] { new Position(4, 4) })
            }
        };

        var playerOne = new RandomAIPlayer(PieceColor.Black, "RandomA", new Random(1234));
        var playerTwo = new RandomAIPlayer(PieceColor.Black, "RandomB", new Random(1234));

        var first = playerOne.ChooseMove(candidates[0]);
        var second = playerTwo.ChooseMove(candidates[0]);

        Assert.Equal(first, second);
        Assert.Contains(first, candidates[0]);
    }

    [Fact]
    public void GameManager_WhenAutoPlayerReturnsAnInvalidMove_ThrowsInvalidOperationException()
    {
        var black = new ScriptedAutoPlayer(PieceColor.Black, "BadBlack", returnInvalidMove: true);
        var white = new HumanPlayer(PieceColor.White, "White");
        var game = new GameManager(black, white);

        Assert.Throws<InvalidOperationException>(() => game.Start());
    }

    [Fact]
    public void GameManager_WithAIvsAIPlay_AlwaysEndsInGameOver()
    {
        foreach (var seed in new[] { 1, 2, 3, 4, 5, 42 })
        {
            var game = new GameManager(
                new RandomAIPlayer(PieceColor.Black, "Black", new Random(seed)),
                new RandomAIPlayer(PieceColor.White, "White", new Random(seed + 101)));

            game.Start();

            Assert.True(game.IsGameOver());
            Assert.NotNull(game.Result);
        }
    }
}
