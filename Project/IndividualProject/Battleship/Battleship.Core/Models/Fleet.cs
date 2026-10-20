namespace Battleship.Core;

public class Fleet : IFleet
{
    public Fleet()
    {
        Ships = new List<IShip>();
    }

    public List<IShip> Ships { get; set; }
}
