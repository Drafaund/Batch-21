using Othello.Core;

namespace Othello.Tests;

public sealed class OthelloCoreTests
{
    [Fact]
    public void Board_Reset_UsesOfficialStartingPosition()
    {
        var board = new Board();

        Assert.Equal(2, board.Count(DiscColor.Black));
        Assert.Equal(2, board.Count(DiscColor.White));
        Assert.Equal(60, board.Count(DiscColor.Empty));
        Assert.Equal(DiscColor.White, board.GetColor(new Position(3, 3)));
        Assert.Equal(DiscColor.Black, board.GetColor(new Position(4, 3)));
    }

    [Fact]
    public void Validator_InitialBlackMoves_AreTheFourExpectedMoves()
    {
        var board = new Board();
        var moves = new OthelloMoveValidator().GetValidMoves(board, DiscColor.Black);

        Assert.Equal(
            [new Position(2, 3), new Position(3, 2), new Position(4, 5), new Position(5, 4)],
            moves);
    }

    [Fact]
    public void GameManager_Start_EmitsInitialStateAndAllowsBlackMove()
    {
        var board = new Board();
        var validator = new OthelloMoveValidator();
        var game = new GameManager(
            board,
            validator,
            new StandardGameEndRule(validator),
            new HumanPlayer(DiscColor.Black, "Black"),
            new HumanPlayer(DiscColor.White, "White"));
        var events = new List<string>();
        game.GameStarted += () => events.Add("started");
        game.ScoreChanged += score => events.Add($"score:{score.Black},{score.White}");
        game.TurnChanged += color => events.Add($"turn:{color}");

        var start = game.Start();
        var move = game.TryPlayMove(new Position(3, 2));

        Assert.True(start.IsSuccess());
        Assert.True(move.IsSuccess());
        Assert.Equal(["started", "score:2,2", "turn:Black", "score:4,1", "turn:White"], events);
        Assert.Equal(new Score(4, 1), game.GetScore());
    }

    [Fact]
    public void MoveResult_IsSuccess_OnlyWhenFailureIsNone()
    {
        Assert.True(new MoveResult(MoveFailureReason.None, []).IsSuccess());
        Assert.False(new MoveResult(MoveFailureReason.CellOccupied, []).IsSuccess());
    }
}
