namespace Othello.Core;

public enum MoveFailureReason
{
    None,
    NotStarted,
    AlreadyStarted,
    GameOver,
    OutOfBounds,
    CellOccupied,
    NoPiecesFlipped,
    ReentrantCall
}
