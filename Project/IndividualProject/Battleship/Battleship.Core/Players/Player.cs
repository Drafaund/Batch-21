namespace Battleship.Core;

public abstract class Player : IPlayer
{
    protected Player(int id, string name, IBoard board, IFleet fleet)
    {
        Id = id;
        Name = name;
        Board = board;
        Fleet = fleet;
    }

    public int Id { get; }
    public string Name { get; set; }
    public IBoard Board { get; set; }
    public IFleet Fleet { get; set; }

    public abstract Coordinate SelectTarget();
    public abstract void ReceiveResult(Coordinate target, ShotResult result);
}
