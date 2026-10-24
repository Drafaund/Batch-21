# Battleship web design

Design for the web version: an ASP.NET Core app that serves a React front end and lets two people play online.
This is the contract that the API (`Battleship.Api`) and the front end (`battleship-web`) are built against.

## Decisions

| Topic | Decision |
|---|---|
| Turn input | `GameController.TakeTurn(Coordinate target)` takes the target from outside (see `battleship.v3.mmd`). `TakeTurn()` asks the strategy and then does the same. |
| Who may shoot | `Battleship.Api` checks that the caller is `GetCurrentPlayer()` before calling `TakeTurn(target)`. |
| Online humans | A small `OnlinePlayer : Player, IShotStrategy` lives in `Battleship.Api`, not in Core. `SelectTarget()` throws (it is never called) and `ReceiveResult()` does nothing. |
| Bots | `RandomAIPlayer` and `HuntTargetAIPlayer` from Core. The server drives them with `TakeTurn()`. |
| Identity | No login. A player gives a name and gets a random token for that game. |
| Commands vs events | Commands go over REST. SignalR only pushes events from the server to the clients. |
| Authority | The server decides everything. A client only says "fire at D-4". |
| Hosting | One app, one Docker image: the API also serves the built React files. No CORS needed. |
| State | In memory, one server instance. A restart or sleep loses running games. |

## Concepts

- **Game**: one `GameSession` holding a `GameController`, two seats, and a lock. It has a short **code** (for example `K7QF2`).
- **Seat**: 1 or 2. Seat 1 is `Player1` and fires first. In a game against a bot, the human is seat 1 and the bot is seat 2.
- **Phases**: `Lobby` (waiting for the second human), `Placing` (ships are being placed), `InProgress`, `Finished`. They map to `GameState.Setup` (Lobby and Placing) and the other two states. Lobby and Placing exist only in the API.
- The game starts by itself when both fleets are complete. There is no separate "ready" call.

## REST API

All bodies are JSON (`camelCase`, enums as strings). A player proves who they are with the header `X-Player-Token`.
Coordinates are `{ "x": 0-9, "y": 0-9 }`: x is the column, y is the row, so `D-4` is `{ "x": 3, "y": 3 }`.

| Method and path | Purpose | Body | Success |
|---|---|---|---|
| `POST /api/games` | Create a game | `{ "playerName", "opponent": "random" \| "huntTarget" \| "human" }` | `201` `{ code, playerToken, seat, view }` |
| `POST /api/games/{code}/join` | Second human joins (`opponent: "human"` games only) | `{ "playerName" }` | `200` `{ playerToken, seat, view }` |
| `GET /api/games/{code}` | Current view, also used to resynchronise after a reconnect | none | `200` `GameView` |
| `POST /api/games/{code}/ships` | Place one ship | `{ "type", "start": {x,y}, "orientation": "Horizontal" \| "Vertical" }` | `200` `GameView` |
| `POST /api/games/{code}/ships/random` | Place all five ships at random (only while the caller has no ship yet) | none | `200` `GameView` |
| `POST /api/games/{code}/shots` | Fire a shot | `{ "target": {x,y} }` | `200` `ShotResponse` |
| `POST /api/games/{code}/emotes` | Send an emote to the other human (rooms only) | `{ "emote": "thumbsUp" }` | `204` |

- **Against a bot**, the bot's ships are placed when the game is created. After a valid human shot the server plays the bot's turns until it is the human's turn again, and returns them in `botShots`.
- **`Invalid` shots** (outside the board or already fired at) return `200` with `result: "Invalid"` and `yourTurn: true`, the same as Core. The shooter fires again.
- **Errors** use RFC 7807 problem details:

| Status | When |
|---|---|
| `400` | Bad body, name outside 1-20 characters, unknown ship type or orientation |
| `401` | Missing or wrong `X-Player-Token` |
| `404` | Unknown code, or the game has expired |
| `409` | Wrong phase, not your turn, room already full, ship type already placed, ship does not fit, emote against a bot or before the opponent has joined |
| `429` | Rate limit or session limit reached, or an emote sent before `Game:EmoteCooldown` has passed |

## DTOs

```jsonc
// GameView: what one player is allowed to see
{
  "code": "K7QF2",
  "version": 12,                       // grows with every change; clients ignore older views
  "phase": "Placing",                  // Lobby | Placing | InProgress | Finished
  "you": { "seat": 1, "name": "Ann" },
  "opponent": { "name": "Bob", "connected": true },   // null in Lobby
  "yourTurn": false,
  "winner": null,                      // "you" | "opponent" | null
  "placement": { "placed": ["Carrier"], "remaining": ["Battleship", "Cruiser", "Submarine", "Destroyer"] },
  "ownBoard":   { "size": 10, "cells": [["Water", "Ship", "Hit", "Miss"]] },   // cells[y][x]
  "ownShips":   [{ "type": "Carrier", "length": 5, "orientation": "Horizontal", "positions": [{"x":0,"y":0}], "hitCount": 0, "isSunk": false }],
  "enemyBoard": { "size": 10, "cells": [["Unknown", "Hit", "Miss", "Sunk"]] },  // cells[y][x]
  "enemyShipsRemaining": 5,
  "endReason": null,                   // "Forfeit" (stayed away too long) | "Timeout" (did not fire in time) | null
  "turnSecondsRemaining": null         // seconds the player to fire has left, when the view was built; only in a battle between two humans
}

// ShotResponse
{ "result": "Hit",                     // Hit | Miss | Sunk | Invalid
  "botShots": [ { "target": {"x":4,"y":2}, "result": "Miss" } ],               // empty unless the opponent is a bot
  "view": { /* GameView */ } }
```

**No leaks.** The enemy board has its own type whose cell values are only `Unknown`, `Hit`, `Miss`, and `Sunk`. None of them says that an unhit ship is there. `Sunk` marks the cells of a ship that was hit on every cell, so it shows nothing the shooter has not already shot at (the same ship was announced as `Sunk` in the shot result). A cell of a ship that is only partly hit stays `Hit`. `enemyShipsRemaining` is a count; the types of the enemy ships are never sent. A test must check that the JSON for the enemy board and for `GameView` contains no enemy ship position.

## Real time (SignalR)

- Hub: `/hubs/game?code={code}` with the token as the access token (the `access_token` query value that the SignalR client sends for WebSockets, or an `Authorization: Bearer` header). The code and token are checked before the connection is negotiated, so a wrong one gets the same `401` or `404` problem details as REST. A connection joins the group of its own seat, `game:{code}:seat1` or `game:{code}:seat2`, so each player gets their own filtered view.
- The server sends events only; the clients send nothing over the hub. Events are sent only in games between two humans; a game against a bot uses the REST answers alone.
- A player may have the game open in two tabs. The opponent is told about a drop only when the last connection of that seat closes.
- Every event carries a full `view` and its `version`, so a client can ignore stale events and simply replace its state. The one exception is `EmoteReceived`: an emote does not change the game, so it has no `view`, does not raise the `version`, and is not stored. A player who was away never sees the emotes sent meanwhile.

| Event | Sent when | Payload |
|---|---|---|
| `PlayerJoined` | The second human joins | `{ name, view }` |
| `GameStarted` | Both fleets are complete | `{ view }` |
| `ShotResolved` | After every shot (from `OnShotResolved`) | `{ shooter: "you" \| "opponent", target, result, view }` |
| `GameEnded` | The game ends (from `OnGameEnded`) | `{ winner: "you" \| "opponent", view }` |
| `OpponentConnection` | The opponent connects or drops | `{ connected, view }` |
| `EmoteReceived` | The opponent sent an emote (only to the other seat, never to the sender) | `{ emote }` |

- An `Invalid` shot sends no event. A shot that ends the game sends `ShotResolved` and then `GameEnded`.
- After a drop the client reconnects (automatically, for two minutes) and calls `GET /api/games/{code}` to resynchronise, because the events sent while it was away are not replayed.
- Events are sent after the game lock is released. Two events of one command carry the same `version`.
- **Placing in a room**: nothing can be placed or fired while the room is in `Lobby` (`409`). Each player places on their own board. The game starts by itself when the second fleet is complete and both players get `GameStarted`.

## Session rules

| Rule | Value |
|---|---|
| Code | 5 characters from `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (no look-alikes), chosen at random and checked for collisions |
| Token | 128 random bits, base64url, one per player per game, never put in an event |
| Player name | Trimmed, 1-20 characters, no control characters. The front end shows it as plain text. |
| Concurrency | One lock per `GameSession`; every command runs under it |
| Idle expiry | A game with no command for 30 minutes is removed; a finished game is removed after 10 minutes |
| Limits | At most 100 games at once; a rate limit per client address |
| Dropped opponent | The other player sees `connected: false`. During a battle the absent player forfeits after `Game:OpponentTimeout` (30 seconds): the game ends, `winner` is the other player, and `endReason` is `Forfeit`. Coming back before that cancels it. In the lobby and while placing ships the game only waits; removing such games is part of the expiry rule above. |
| Emotes | Only between two humans, in any phase after the opponent has joined (also after the game is over, for a "good game"). The server accepts only the codes of `EmoteCatalog`: `thumbsUp`, `laugh`, `wow`, `angry`, `target`, `wave`, `handshake`, `clap`; anything else is `400`, so nothing a player types reaches the other. Each seat waits `Game:EmoteCooldown` (2 seconds) between two emotes (`429`). The front end turns a code into a picture, shows the emote of the opponent for 3 seconds, and lets a player hide them (a per-browser choice, nothing on the server). |
| Turn time | In a battle between two humans, the player to fire has `Game:TurnTimeout` (30 seconds). When it runs out the game ends, `winner` is the other player, and `endReason` is `Timeout`. A valid shot starts the clock of the other player; an `Invalid` shot does not restart it. There is no clock against a bot, in the lobby, or while placing ships. Both values are set in the `Game` section of `appsettings.json` (or the environment, for example `Game__TurnTimeout=00:00:20`). |
| Server restart or sleep | Games are lost. The client gets `404` and shows "this game no longer exists". |

## Pieces inside `Battleship.Api`

- `GameSession`: the controller, the two seats, the lock, the phase, the version, the events to send, and the last activity time.
- `Seat`: one side of a game: its player, its token, its open connections, and the clock that runs while the player is away.
- `GameHub` and `HubAuthentication`: the SignalR hub and the check of code and token before a connection opens. `HubGameEventSink` sends what a session collected.
- `GameStore`: the in-memory dictionary of sessions, code generation, limits, and expiry (a hosted background service).
- `OnlinePlayer`: the human seat described above.
- `ViewMapper`: turns a `GameSession` into a `GameView` for one seat. This is the only place that decides what a player may see.
- Endpoints and the SignalR hub: thin; they check the token and the turn, call the session, and send events.

## Status

| Part | State |
|---|---|
| `TakeTurn(Coordinate target)` in Core | Done (`battleship.v3.mmd`, waiting for review) |
| REST API against a bot: create, get, place ships, place at random, fire | Done and tested (`Battleship.Api`) |
| `opponent: "human"`, `join`, SignalR, the `Lobby` phase, the opponent `connected` flag, forfeit after a drop | Done and tested (`Battleship.Api`, two SignalR clients in the tests) |
| Rate limit, session limit, expiry of old games, `429` | Not yet (hardening) |
| React front end against a bot (`battleship-web`) | Done and tested (start, placement, battle, resume after reload) |
| React front end for a room: create or join with a code, lobby, real-time battle, reconnect | Done and tested (unit tests with a fake SignalR, and Playwright with two browsers). Not yet tried over two real networks |
| Enemy board shows sunk ships (`Sunk`), skull for sunk cells and flames for hit cells in the React boards | Done and tested (`Battleship.Api.Tests`, component tests). Rendered once in Chromium (dark and light) from a static page; not yet played in the real app |
| Emotes between two humans: endpoint, `EmoteReceived`, cooldown, picker and bubble in the React battle screen | Done and tested (API and hub tests; unit tests for the hook and the panel). Not covered by Playwright yet |
| Docker, hosting | Not yet |

The API also answers `404`, `405`, and `415` from the framework as problem details, and logs rejected requests at information level (not as errors).

## Out of scope for now

Accounts, a database, spectators, free-text chat, reactions to a single shot, a history of emotes, rematch, and more than one server instance.
