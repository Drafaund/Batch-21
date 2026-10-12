namespace Othello.Core;

public sealed record Move(Position Target, DiscColor Color, IReadOnlyList<Position> Flipped);
