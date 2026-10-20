using Battleship.Core;

namespace Battleship.ConsoleApp;

/// <summary>One side of the game as the console sees it: the player, its shot strategy, and the ships it still has to place.</summary>
public record Participant(IPlayer Player, IShotStrategy Strategy, bool IsHuman, IReadOnlyList<IShip> Ships);
