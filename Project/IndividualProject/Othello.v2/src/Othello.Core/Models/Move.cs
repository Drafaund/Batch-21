namespace Othello.Core;

public sealed record Move(Position Target, PieceColor Color, IReadOnlyList<Position> Flipped);
