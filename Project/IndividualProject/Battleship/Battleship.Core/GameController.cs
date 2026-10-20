namespace Battleship.Core;

/// <summary>
/// Facade for one two-player game: setup, turns, shots, and the winner. All game rules live here.
/// The class is split by stage: this file (state and queries), Setup, Turn, and Rules.
/// </summary>
public partial class GameController
{
    private readonly IShotStrategy _strategy1;
    private readonly IShotStrategy _strategy2;
    private IPlayer _currentPlayer;
    private int _turnNumber;

    /// <summary>Creates a game in the <see cref="GameState.Setup"/> state; <paramref name="player1"/> moves first.</summary>
    /// <param name="player1">The first player.</param>
    /// <param name="strategy1">Shot strategy of <paramref name="player1"/> (usually the same object).</param>
    /// <param name="player2">The second player; must be a different object from <paramref name="player1"/>.</param>
    /// <param name="strategy2">Shot strategy of <paramref name="player2"/> (usually the same object).</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException">Both players are the same object.</exception>
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
    }

    /// <summary>The player who moves first.</summary>
    public IPlayer Player1 { get; }

    /// <summary>The player who moves second.</summary>
    public IPlayer Player2 { get; }

    /// <summary>The current phase of the game.</summary>
    public GameState State { get; private set; }

    /// <summary>Raised after every shot, including <see cref="ShotResult.Invalid"/> ones.</summary>
    public event Action<IPlayer, Coordinate, ShotResult>? OnShotResolved;

    /// <summary>Raised once, with the winner, when the game ends.</summary>
    public event Action<IPlayer>? OnGameEnded;

    /// <summary>The player whose turn it is.</summary>
    public IPlayer GetCurrentPlayer() => _currentPlayer;

    /// <summary>The current phase of the game.</summary>
    public GameState GetState() => State;

    /// <summary>The board of <paramref name="player"/>.</summary>
    /// <exception cref="ArgumentException">The player is not part of this game.</exception>
    public IBoard GetBoard(IPlayer player)
    {
        EnsureParticipant(player);
        return player.Board;
    }

    /// <summary>The fleet of <paramref name="player"/>.</summary>
    /// <exception cref="ArgumentException">The player is not part of this game.</exception>
    public IFleet GetFleet(IPlayer player)
    {
        EnsureParticipant(player);
        return player.Fleet;
    }

    private static int ShipTypeCount => Enum.GetValues<ShipType>().Length;

    private static bool IsInside(IBoard board, Coordinate position)
    {
        return position.X >= 0 && position.X < board.BoardSize
            && position.Y >= 0 && position.Y < board.BoardSize;
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
