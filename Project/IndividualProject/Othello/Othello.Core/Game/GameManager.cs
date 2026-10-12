namespace Othello.Core;

public sealed class GameManager : IGameEvents
{
    private readonly IBoard _board;
    private readonly IMoveValidator _validator;
    private readonly IGameEndRule _endRule;
    private readonly IPlayer _blackPlayer;
    private readonly IPlayer _whitePlayer;
    private bool _isStarted;
    private bool _isProcessing;
    public DiscColor CurrentTurn { get; private set; } = DiscColor.Empty;
    public GameResult? Result { get; private set; }
    public event Action? GameStarted;
    public event Action<Move>? MovePlayed;
    public event Action<Score>? ScoreChanged;
    public event Action<DiscColor>? TurnChanged;
    public event Action<DiscColor>? TurnSkipped;
    public event Action<GameResult>? GameOver;

    public GameManager(IBoard board, IMoveValidator validator, IGameEndRule endRule, IPlayer blackPlayer, IPlayer whitePlayer)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _endRule = endRule ?? throw new ArgumentNullException(nameof(endRule));
        _blackPlayer = blackPlayer ?? throw new ArgumentNullException(nameof(blackPlayer));
        _whitePlayer = whitePlayer ?? throw new ArgumentNullException(nameof(whitePlayer));
        if (_blackPlayer.Color != DiscColor.Black) throw new ArgumentException("Black player must have Black color.", nameof(blackPlayer));
        if (_whitePlayer.Color != DiscColor.White) throw new ArgumentException("White player must have White color.", nameof(whitePlayer));
    }
    public bool IsGameOver() => Result.HasValue;
    public MoveResult Start()
    {
        if (_isProcessing) return Failure(MoveFailureReason.ReentrantCall);
        if (_isStarted) return Failure(MoveFailureReason.AlreadyStarted);
        _isProcessing = true;
        try
        {
            _isStarted = true; _board.Reset(); CurrentTurn = DiscColor.Black;
            GameStarted?.Invoke(); ScoreChanged?.Invoke(GetScore()); TurnChanged?.Invoke(CurrentTurn);
            var moves = new List<Move>(); PlayAutoTurns(moves);
            return Success(moves);
        }
        finally { _isProcessing = false; }
    }
    public MoveResult TryPlayMove(Position position)
    {
        if (!_isStarted) return Failure(MoveFailureReason.NotStarted);
        if (_isProcessing) return Failure(MoveFailureReason.ReentrantCall);
        if (IsGameOver()) return Failure(MoveFailureReason.GameOver);
        if (!_board.IsInside(position)) return Failure(MoveFailureReason.OutOfBounds);
        if (_board.GetColor(position) != DiscColor.Empty) return Failure(MoveFailureReason.CellOccupied);
        var flips = _validator.GetFlips(_board, position, CurrentTurn);
        if (flips.Count == 0) return Failure(MoveFailureReason.NoDiscsFlipped);
        _isProcessing = true;
        try
        {
            var moves = new List<Move> { ApplyMove(position, CurrentTurn) };
            AdvanceTurn(); PlayAutoTurns(moves);
            return Success(moves);
        }
        finally { _isProcessing = false; }
    }
    public IReadOnlyList<Position> GetValidMoves() => !_isStarted || IsGameOver() ? [] : _validator.GetValidMoves(_board, CurrentTurn);
    public Score GetScore() => new(_board.Count(DiscColor.Black), _board.Count(DiscColor.White));

    private void PlayAutoTurns(List<Move> moves)
    {
        while (!IsGameOver() && GetPlayer(CurrentTurn) is IAutoPlayer auto)
        {
            var validMoves = GetValidMoves();
            if (validMoves.Count == 0) break;
            var position = auto.ChooseMove(_board, validMoves);
            if (!validMoves.Contains(position)) throw new InvalidOperationException("Auto player returned an invalid move.");
            moves.Add(ApplyMove(position, CurrentTurn)); AdvanceTurn();
        }
    }
    private Move ApplyMove(Position position, DiscColor color)
    {
        var flipped = _validator.GetFlips(_board, position, color);
        _board.SetColor(position, color); _board.Flip(flipped, color);
        var move = new Move(position, color, flipped);
        MovePlayed?.Invoke(move); ScoreChanged?.Invoke(GetScore()); return move;
    }
    private void AdvanceTurn()
    {
        if (_endRule.TryGetResult(_board, out var result)) { Result = result; GameOver?.Invoke(result); return; }
        var next = CurrentTurn == DiscColor.Black ? DiscColor.White : DiscColor.Black;
        if (_validator.HasValidMove(_board, next)) { CurrentTurn = next; TurnChanged?.Invoke(next); }
        else { TurnSkipped?.Invoke(next); TurnChanged?.Invoke(CurrentTurn); }
    }
    private IPlayer GetPlayer(DiscColor color) => color == DiscColor.Black ? _blackPlayer : _whitePlayer;
    private static MoveResult Failure(MoveFailureReason reason) => new(reason, []);
    private static MoveResult Success(IReadOnlyList<Move> moves) => new(MoveFailureReason.None, moves);
}
