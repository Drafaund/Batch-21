namespace Battleship.Api;

/// <summary>
/// What a player sees on the enemy board. There is deliberately no value for a ship, so this type
/// cannot describe where enemy ships are.
/// </summary>
internal enum EnemyCell
{
    Unknown,
    Hit,
    Miss,
}
