using System.Security.Cryptography;
using System.Text;
using Battleship.Core;

namespace Battleship.Api;

internal sealed class Seat
{
    private byte[]? _tokenBytes;

    public Seat(int number, Player player, bool isBot)
    {
        Number = number;
        Player = player;
        IsBot = isBot;
    }

    public int Number { get; }

    public Player Player { get; }

    public bool IsBot { get; }

    public string? Token { get; private set; }

    public bool IsTaken => IsBot || Token != null;

    public int Connections { get; set; }

    public Timer? DropTimer { get; set; }

    public DateTime? LastEmoteAt { get; set; }

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
