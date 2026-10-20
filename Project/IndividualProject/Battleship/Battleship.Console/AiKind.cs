namespace Battleship.ConsoleApp;

/// <summary>Which bot to play against.</summary>
public enum AiKind
{
    /// <summary>Fires at random cells.</summary>
    Random,
    /// <summary>Hunts at random, then fires at the neighbours of a hit until the ship sinks.</summary>
    HuntTarget,
}
