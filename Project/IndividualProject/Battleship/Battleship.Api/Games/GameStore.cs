using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Battleship.Api;

internal sealed class GameStore
{
    // No 0, O, 1, I, or L, so a code is easy to read out loud.
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;
    private const int TokenBytes = 16;

    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly GameSessionFactory _factory;

    public GameStore(GameSessionFactory factory)
    {
        _factory = factory;
    }

    public int Count => _sessions.Count;

    public GameSession Create(string playerName, OpponentKind opponent)
    {
        while (true)
        {
            var code = NewCode();
            var session = _factory.Create(code, NewToken(), playerName, opponent);
            if (_sessions.TryAdd(code, session))
            {
                return session;
            }
        }
    }

    public GameSession Find(string code)
    {
        if (_sessions.TryGetValue(code.Trim().ToUpperInvariant(), out var session))
        {
            return session;
        }

        throw ApiException.NotFound("There is no game with this code.");
    }

    public GameAccess GetAuthorized(string code, string? token)
    {
        var session = Find(code);
        if (session.FindSeat(token) is not { } seat)
        {
            throw ApiException.Unauthorized("The X-Player-Token header is missing or wrong for this game.");
        }

        return new GameAccess(session, seat);
    }

    public JoinGameResponse Join(string code, string playerName)
    {
        return Find(code).Join(playerName, NewToken());
    }

    private static string NewCode()
    {
        return string.Create(CodeLength, 0, static (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            }
        });
    }

    private static string NewToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
}
