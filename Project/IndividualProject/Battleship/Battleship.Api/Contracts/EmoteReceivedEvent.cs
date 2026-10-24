namespace Battleship.Api;

/// <summary>Sent to the opponent of the player who sent an emote. It carries no view: an emote does not change the game.</summary>
internal sealed record EmoteReceivedEvent(string Emote);
