namespace Battleship.Core;

public interface IPlayer
{
    int Id { get; }
    string Name { get; set; }
    IBoard Board { get; set; }
    IFleet Fleet { get; set; }
}
