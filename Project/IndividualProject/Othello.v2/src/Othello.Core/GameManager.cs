namespace Othello.Core;

public sealed class GameManager
{
    private readonly Player _blackPlayer;
    private readonly Player _whitePlayer;
    private readonly IReadOnlyList<Direction> _directions;
    private bool _isStarted;
    private bool _isProcessing;

    public GameManager(Player blackPlayer, Player whitePlayer)
        : this(blackPlayer, whitePlayer, new Board(), PieceColor.Black)
    {
        PlaceInitialPieces();
    }

    internal GameManager(Player blackPlayer, Player whitePlayer, Board board, PieceColor firstTurn = PieceColor.Black)
    {
        ArgumentNullException.ThrowIfNull(blackPlayer);
        ArgumentNullException.ThrowIfNull(whitePlayer);
        ArgumentNullException.ThrowIfNull(board);

        if (blackPlayer.Color != PieceColor.Black || whitePlayer.Color != PieceColor.White)
        {
            throw new InvalidOperationException("Black player must be black and white player must be white.");
        }

        _blackPlayer = blackPlayer;
        _whitePlayer = whitePlayer;
        Board = board;
        CurrentTurn = firstTurn;
        _directions = new[]
        {
            new Direction(-1, -1),
            new Direction(-1, 0),
            new Direction(-1, 1),
            new Direction(0, -1),
            new Direction(0, 1),
            new Direction(1, -1),
            new Direction(1, 0),
            new Direction(1, 1)
        };
    }

    public Board Board { get; }
    public PieceColor CurrentTurn { get; private set; }
    public GameResult? Result { get; private set; }

    public event Action? GameStarted;
    public event Action<Move>? MovePlayed;
    public event Action<Score>? ScoreChanged;
    public event Action<PieceColor>? TurnChanged;
    public event Action<PieceColor>? TurnSkipped;
    public event Action<GameResult>? GameOver;

    public bool IsGameOver()
    {
        return Result.HasValue;
    }

    public MoveResult Start()
    {
        if (_isProcessing)
        {
            return new MoveResult(MoveFailureReason.ReentrantCall, Array.Empty<Move>());
        }

        _isProcessing = true;
        try
        {
            if (_isStarted)
            {
                return new MoveResult(MoveFailureReason.AlreadyStarted, Array.Empty<Move>());
            }

            _isStarted = true;
            GameStarted?.Invoke();
            ScoreChanged?.Invoke(GetScore());
            TurnChanged?.Invoke(CurrentTurn);

            var moves = new List<Move>();
            PlayAutoTurns(moves);
            return new MoveResult(MoveFailureReason.None, moves);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    public MoveResult TryPlayMove(Position position)
    {
        if (_isProcessing)
        {
            return new MoveResult(MoveFailureReason.ReentrantCall, Array.Empty<Move>());
        }

        if (!_isStarted)
        {
            return new MoveResult(MoveFailureReason.NotStarted, Array.Empty<Move>());
        }

        if (Result.HasValue)
        {
            return new MoveResult(MoveFailureReason.GameOver, Array.Empty<Move>());
        }

        if (!IsInside(position))
        {
            return new MoveResult(MoveFailureReason.OutOfBounds, Array.Empty<Move>());
        }

        var cell = GetCell(position);
        if (cell.Piece is not null)
        {
            return new MoveResult(MoveFailureReason.CellOccupied, Array.Empty<Move>());
        }

        var flips = GetFlips(position, CurrentTurn);
        if (flips.Count == 0)
        {
            return new MoveResult(MoveFailureReason.NoPiecesFlipped, Array.Empty<Move>());
        }

        _isProcessing = true;
        try
        {
            var move = ApplyMove(position, CurrentTurn);
            var results = new List<Move> { move };

            MovePlayed?.Invoke(move);
            ScoreChanged?.Invoke(GetScore());
            AdvanceTurn();
            PlayAutoTurns(results);

            return new MoveResult(MoveFailureReason.None, results);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    public IReadOnlyList<Position> GetValidMoves()
    {
        if (!_isStarted || Result.HasValue)
        {
            return Array.Empty<Position>();
        }

        return FindValidMoves(CurrentTurn)
            .Select(move => move.Target)
            .ToArray();
    }

    public Score GetScore()
    {
        var black = Board.Cells.Count(cell => cell.Piece is not null && cell.Piece.Color == PieceColor.Black);
        var white = Board.Cells.Count(cell => cell.Piece is not null && cell.Piece.Color == PieceColor.White);
        return new Score(black, white);
    }

    private void PlaceInitialPieces()
    {
        PlacePiece(new Position(3, 3), PieceColor.White);
        PlacePiece(new Position(4, 4), PieceColor.White);
        PlacePiece(new Position(3, 4), PieceColor.Black);
        PlacePiece(new Position(4, 3), PieceColor.Black);
    }

    private void PlacePiece(Position position, PieceColor color)
    {
        var cell = GetCell(position);
        cell.Piece = new Piece(color);
    }

    private void PlayAutoTurns(List<Move> moves)
    {
        while (!Result.HasValue)
        {
            var player = GetPlayer(CurrentTurn);
            if (player is not IAutoPlayer autoPlayer)
            {
                return;
            }

            var candidates = FindValidMoves(CurrentTurn);
            if (candidates.Count == 0)
            {
                return;
            }

            var move = autoPlayer.ChooseMove(candidates);
            if (!candidates.Contains(move))
            {
                throw new InvalidOperationException("AI player returned a move that is not a valid candidate.");
            }

            var appliedMove = ApplyMove(move.Target, CurrentTurn);
            moves.Add(appliedMove);
            MovePlayed?.Invoke(appliedMove);
            ScoreChanged?.Invoke(GetScore());
            AdvanceTurn();
        }
    }

    private Move ApplyMove(Position position, PieceColor color)
    {
        var flips = GetFlips(position, color);
        var cell = GetCell(position);
        var piece = new Piece(color);

        cell.Piece = piece;
        foreach (var flippedPosition in flips)
        {
            GetCell(flippedPosition).Piece = piece;
        }

        return new Move(position, color, flips);
    }

    private void AdvanceTurn()
    {
        var opponent = CurrentTurn == PieceColor.Black ? PieceColor.White : PieceColor.Black;

        if (!HasValidMove(CurrentTurn) && !HasValidMove(opponent))
        {
            if (!TryGetResult(out var result))
            {
                return;
            }

            Result = result;
            GameOver?.Invoke(Result.Value);
            return;
        }

        if (HasValidMove(opponent))
        {
            CurrentTurn = opponent;
            TurnChanged?.Invoke(CurrentTurn);
            return;
        }

        TurnSkipped?.Invoke(opponent);
        TurnChanged?.Invoke(CurrentTurn);
    }

    private bool HasValidMove(PieceColor color)
    {
        return FindValidMoves(color).Count > 0;
    }

    private List<Move> FindValidMoves(PieceColor color)
    {
        var validMoves = new List<Move>();

        foreach (var cell in Board.Cells)
        {
            if (cell.Piece is not null)
            {
                continue;
            }

            var flips = GetFlips(cell.Position, color);
            if (flips.Count > 0)
            {
                validMoves.Add(new Move(cell.Position, color, flips));
            }
        }

        return validMoves;
    }

    private List<Position> GetFlips(Position position, PieceColor color)
    {
        if (!IsInside(position))
        {
            return new List<Position>();
        }

        var cell = GetCell(position);
        if (cell.Piece is not null)
        {
            return new List<Position>();
        }

        var flips = new List<Position>();

        foreach (var direction in _directions)
        {
            var current = new Position(position.Row + direction.DRow, position.Col + direction.DCol);
            var positions = new List<Position>();

            while (IsInside(current))
            {
                var candidateCell = GetCell(current);
                if (candidateCell.Piece is null)
                {
                    break;
                }

                if (candidateCell.Piece.Color == color)
                {
                    if (positions.Count > 0)
                    {
                        flips.AddRange(positions);
                    }

                    break;
                }

                positions.Add(current);
                current = new Position(current.Row + direction.DRow, current.Col + direction.DCol);
            }
        }

        return flips;
    }

    private bool IsInside(Position position)
    {
        return position.Row >= 0
            && position.Row < Board.Size
            && position.Col >= 0
            && position.Col < Board.Size;
    }

    private Cell GetCell(Position position)
    {
        return Board.Cells[position.Row * Board.Size + position.Col];
    }

    private Player GetPlayer(PieceColor color)
    {
        return color == PieceColor.Black ? _blackPlayer : _whitePlayer;
    }

    private bool TryGetResult(out GameResult result)
    {
        var score = GetScore();
        if (score.Black > score.White)
        {
            result = GameResult.BlackWins;
            return true;
        }

        if (score.White > score.Black)
        {
            result = GameResult.WhiteWins;
            return true;
        }

        result = GameResult.Draw;
        return true;
    }
}
