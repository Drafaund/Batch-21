using Battleship.Core;

namespace Battleship.ConsoleApp;

public record Participant(IPlayer Player, IShotStrategy Strategy, bool IsHuman, IReadOnlyList<IShip> Ships);
