namespace Battleship.Core;

/// <summary>A participant of the game: an identity with its own board and fleet.</summary>
public interface IPlayer
{
    /// <summary>A number that identifies the player.</summary>
    int Id { get; }
    /// <summary>The display name.</summary>
    string Name { get; set; }
    /// <summary>The own board of the player.</summary>
    IBoard Board { get; set; }
    /// <summary>The own fleet of the player.</summary>
    IFleet Fleet { get; set; }
}
