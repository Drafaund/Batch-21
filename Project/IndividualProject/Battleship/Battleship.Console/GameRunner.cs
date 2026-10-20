using Battleship.Core;

namespace Battleship.ConsoleApp;

/// <summary>Drives one console game: ship placement, then turns until there is a winner.</summary>
public class GameRunner
{
    private readonly GameController _controller;
    private readonly Participant _first;
    private readonly Participant _second;
    private readonly ConsoleScreen _screen;
    private readonly BoardRenderer _renderer;
    private readonly PlacementPhase _placement;
    private readonly bool _hotSeat;
    private readonly bool _botsOnly;

    public GameRunner(
        GameController controller,
        Participant first,
        Participant second,
        ConsoleScreen screen,
        BoardRenderer renderer,
        PlacementPhase placement)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(placement);
        _controller = controller;
        _first = first;
        _second = second;
        _screen = screen;
        _renderer = renderer;
        _placement = placement;
        _hotSeat = first.IsHuman && second.IsHuman;
        _botsOnly = !first.IsHuman && !second.IsHuman;
    }

    public void Run()
    {
        PlaceFleets();
        _controller.StartGame();
        PlayTurns();
        ShowFinalBoards();
    }

    private void PlaceFleets()
    {
        foreach (var participant in new[] { _first, _second })
        {
            if (_hotSeat)
            {
                HandOver($"{participant.Player.Name}, place your ships. {Other(participant).Player.Name}, look away.");
            }

            _placement.Place(participant);

            if (_hotSeat)
            {
                _screen.WaitForEnter("Press Enter to hide your board...");
                _screen.Clear();
            }
        }
    }

    private void PlayTurns()
    {
        IPlayer? lastPlayer = null;
        while (_controller.GetState() == GameState.InProgress)
        {
            var current = _controller.GetCurrentPlayer();
            var participant = Of(current);

            // A shot that was Invalid keeps the turn, so the same player is asked again without redrawing.
            if (!ReferenceEquals(current, lastPlayer))
            {
                BeginTurn(participant);
                lastPlayer = current;
            }

            var result = _controller.TakeTurn();
            EndTurn(participant, result);
        }
    }

    private void BeginTurn(Participant participant)
    {
        if (_botsOnly)
        {
            _screen.Clear();
            _renderer.ShowFleets(_first.Player.Name, BoardOf(_first), _second.Player.Name, BoardOf(_second));
            _screen.WriteLine();
            return;
        }

        if (!participant.IsHuman)
        {
            return;
        }

        if (_hotSeat)
        {
            HandOver($"{participant.Player.Name}, it is your turn. {Other(participant).Player.Name}, look away.");
        }
        else
        {
            _screen.WriteLine(new string('-', 40));
        }

        ShowTurnView(participant);
    }

    private void EndTurn(Participant participant, ShotResult result)
    {
        if (!participant.IsHuman)
        {
            _screen.PauseForBot();
            return;
        }

        if (_hotSeat && result != ShotResult.Invalid && _controller.GetState() == GameState.InProgress)
        {
            ShowTurnView(participant);
            _screen.WaitForEnter("Press Enter to end your turn...");
        }
    }

    private void ShowTurnView(Participant participant)
    {
        var other = Other(participant);
        _screen.WriteLine($"{participant.Player.Name}'s turn. Ships left: you {_controller.GetRemainingShips(participant.Player)}, enemy {_controller.GetRemainingShips(other.Player)}.");
        _renderer.ShowPlayerView(BoardOf(participant), BoardOf(other));
    }

    private void ShowFinalBoards()
    {
        _screen.WriteLine();
        _renderer.ShowFleets(_first.Player.Name, BoardOf(_first), _second.Player.Name, BoardOf(_second));
    }

    /// <summary>Clears the screen and waits so the next player can sit down before their board is shown.</summary>
    private void HandOver(string message)
    {
        _screen.Clear();
        _screen.WaitForEnter($"{message} Press Enter when ready...");
        _screen.Clear();
    }

    private IBoard BoardOf(Participant participant) => _controller.GetBoard(participant.Player);

    private Participant Of(IPlayer player) => ReferenceEquals(player, _first.Player) ? _first : _second;

    private Participant Other(Participant participant) => ReferenceEquals(participant, _first) ? _second : _first;
}
