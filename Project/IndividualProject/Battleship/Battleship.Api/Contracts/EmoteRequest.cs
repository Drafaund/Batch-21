namespace Battleship.Api;

/// <summary>Body of <c>POST /api/games/{code}/emotes</c>.</summary>
internal sealed record EmoteRequest(string? Emote);
