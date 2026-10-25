namespace Battleship.Core;

public class HumanPlayer : Player, IShotStrategy
{
    private readonly ICoordinateInput _input;

    public HumanPlayer(int id, string name, IBoard board, IFleet fleet, ICoordinateInput input)
        : base(id, name, board, fleet)
    {
        ArgumentNullException.ThrowIfNull(input);
        _input = input;
    }

    public Coordinate SelectTarget()
    {
        return _input.ReadCoordinate();
    }

    public void ReceiveResult(Coordinate target, ShotResult result)
    {
    }
}
