using System.Security.Cryptography;
using System.Text;
using Battleship.Core;

namespace Battleship.Api;

/// <summary>
/// One side of a game: its player, the secret token that proves who is calling, and how many live connections it has.
/// The second seat of an online game starts empty and is filled when the second human joins.
/// </summary>
internal sealed class Seat
{
    private byte[]? _tokenBytes;

    public Seat(int number, Player player, bool isBot)
    {
        Number = number;
        Player = player;
        IsBot = isBot;
    }

    /// <summary>1 or 2. Seat 1 fires first.</summary>
    public int Number { get; }

    public Player Player { get; }

    public bool IsBot { get; }

    /// <summary>The token of the human in this seat; null for a bot and for an empty seat.</summary>
    public string? Token { get; private set; }

    /// <summary>False only for the second seat of an online game that nobody has joined yet.</summary>
    public bool IsTaken => IsBot || Token != null;

    /// <summary>Open SignalR connections of this seat. A player may have the game open in two tabs.</summary>
    public int Connections { get; set; }

    /// <summary>Counts down while the player is away during a game; at zero the player forfeits.</summary>
    public Timer? DropTimer { get; set; }

    /// <summary>When this seat last sent an emote, for the cooldown; null before the first one.</summary>
    public DateTime? LastEmoteAt { get; set; }

    /// <summary>A bot is always there; a human is there while at least one connection is open.</summary>
    public bool IsConnected => IsBot || Connections > 0;

    public void Take(string name, string token)
    {
        Player.Name = name;
        Token = token;
        _tokenBytes = Encoding.UTF8.GetBytes(token);
    }

    public bool Authorizes(string? token)
    {
        return token != null && _tokenBytes != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), _tokenBytes);
    }

    public void StopDropTimer()
    {
        DropTimer?.Dispose();
        DropTimer = null;
    }
}
