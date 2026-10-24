namespace Battleship.Api;

/// <summary>
/// What a player sees on the enemy board. There is deliberately no value for a ship that is still afloat, so this
/// type cannot describe where the enemy ships are. <see cref="Sunk"/> only marks the cells of a ship that was hit on
/// every cell, so it shows nothing the shooter has not already shot at.
/// </summary>
internal enum EnemyCell
{
    Unknown,
    Hit,
    Miss,
    Sunk,
}
