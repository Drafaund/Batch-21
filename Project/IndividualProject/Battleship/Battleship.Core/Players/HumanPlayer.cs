namespace Battleship.Core;

/// <summary>A player whose targets come from an <see cref="ICoordinateInput"/> (console, web, ...).</summary>
public class HumanPlayer : Player, IShotStrategy
{
    private readonly ICoordinateInput _input;

    public HumanPlayer(int id, string name, IBoard board, IFleet fleet, ICoordinateInput input)
        : base(id, name, board, fleet)
    {
        ArgumentNullException.ThrowIfNull(input);
        _input = input;
    }

    /// <summary>Asks the input source for the next target.</summary>
    public Coordinate SelectTarget()
    {
        return _input.ReadCoordinate();
    }

    /// <summary>A human sees the result on screen, so there is nothing to remember here.</summary>
    public void ReceiveResult(Coordinate target, ShotResult result)
    {
    }
}
