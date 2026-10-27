namespace Battleship.Core;

public partial class GameController
{
    private readonly IShotStrategy _strategy1;
    private readonly IShotStrategy _strategy2;
    private IPlayer _currentPlayer;
    private int _turnNumber;

    public GameController(IPlayer player1, IShotStrategy strategy1, IPlayer player2, IShotStrategy strategy2)
    {
        ArgumentNullException.ThrowIfNull(player1);
        ArgumentNullException.ThrowIfNull(strategy1);
        ArgumentNullException.ThrowIfNull(player2);
        ArgumentNullException.ThrowIfNull(strategy2);
        if (ReferenceEquals(player1, player2))
        {
            throw new ArgumentException("Player1 and Player2 must be different players.", nameof(player2));
        }

        Player1 = player1;
        Player2 = player2;
        _strategy1 = strategy1;
        _strategy2 = strategy2;
        _currentPlayer = player1;
        _turnNumber = 1;
        State = GameState.Setup;

        Board = new Dictionary<IPlayer, IBoard>
        {
            [player1] = player1.Board,
            [player2] = player2.Board,
        };
        Fleet = new Dictionary<IPlayer, IFleet>
        {
            [player1] = player1.Fleet,
            [player2] = player2.Fleet,
        };
    }

    public IPlayer Player1 { get; }

    public IPlayer Player2 { get; }

    public Dictionary<IPlayer, IBoard> Board { get; }

    public Dictionary<IPlayer, IFleet> Fleet { get; }

    public GameState State { get; private set; }

    public event Action<IPlayer, Coordinate, ShotResult>? OnShotResolved;

    public event Action<IPlayer>? OnGameEnded;

    public IPlayer GetCurrentPlayer() => _currentPlayer;

    public GameState GetState() => State;

    public IBoard GetBoard(IPlayer player)
    {
        EnsureParticipant(player);
        return Board[player];
    }

    public IFleet GetFleet(IPlayer player)
    {
        EnsureParticipant(player);
        return Fleet[player];
    }

    private static int ShipTypeCount => Enum.GetValues<ShipType>().Length;

    private static bool IsInside(IBoard board, Coordinate position)
    {
        var isColumnInside = position.X >= 0 && position.X < board.BoardSize;
        var isRowInside = position.Y >= 0 && position.Y < board.BoardSize;

        return isColumnInside && isRowInside;
    }

    private IPlayer GetOpponent(IPlayer player)
    {
        return ReferenceEquals(player, Player1) ? Player2 : Player1;
    }

    private void EnsureParticipant(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!ReferenceEquals(player, Player1) && !ReferenceEquals(player, Player2))
        {
            throw new ArgumentException("The player is not part of this game.", nameof(player));
        }
    }

    private void EnsureInProgress()
    {
        if (State != GameState.InProgress)
        {
            throw new InvalidOperationException("The game is not in progress.");
        }
    }
}
