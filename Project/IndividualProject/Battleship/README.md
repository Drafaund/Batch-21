# Battleship

A two-player Battleship game written in C# (.NET 8) with a React front end. It runs in the terminal and in the browser, against a bot or against another person.
The game rules live in a UI-free library (`Battleship.Core`), so the console app, the web API, and the web page all use the same code.

| Where | What you can do |
|---|---|
| Terminal | Human vs Human on one keyboard, Human vs AI, AI vs AI |
| Browser | Play against a bot, or play online against a friend in a room with a code, with real-time updates (SignalR) |

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An interactive terminal for the console app (the menus use arrow keys, so piped input is not supported)
- [Node.js 22](https://nodejs.org/) for the web version

## Run

From this folder (`Battleship`):

```
dotnet run --project Battleship.Console
```

### Web version

```
# terminal 1, from this folder
dotnet run --project Battleship.Api

# terminal 2
cd battleship-web
npm install
npm run dev
```

Open http://localhost:5173. The API listens on http://localhost:5080, and in the Development environment its interactive documentation is at http://localhost:5080/swagger. The endpoints, JSON shapes, and rules are described in [WEB_DESIGN.md](WEB_DESIGN.md), and the front end is described in [battleship-web/README.md](battleship-web/README.md).

**Against a bot:** enter a name, choose *Random* or *Hunt-Target*, place your ships by hand or at random, and fire.

**Online against a person:** one player chooses *Create a room* and shares the five-character code. The other player enters a name and the code and chooses *Join room*. Both place their ships, and the game starts by itself when both fleets are complete. To try it alone, open the page in a second browser window (for example a private window).

Online games have a few extra rules:

- A player who does not fire within 30 seconds of their turn loses. A countdown is shown.
- A player who stays disconnected for 30 seconds during a battle loses; the other player sees when they leave and when they come back.
- Players can send each other one of eight emotes (2 seconds between emotes). The server only accepts codes from a fixed list, so no free text reaches the opponent.
- Reloading the page returns to the same game.

The limits are set in the `Game` section of `Battleship.Api/appsettings.json` (`TurnTimeout`, `OpponentTimeout`, `EmoteCooldown`), or with environment variables such as `Game__TurnTimeout=00:00:20`.

Games live in memory and are lost when the server stops. Finished and abandoned games are not cleaned up yet (see [Planned](#planned)).

## Game modes (terminal)

| Mode | Description |
|---|---|
| Human vs Human | Two people share one keyboard. The screen is cleared between turns so nobody sees the other's fleet. |
| Human vs AI | One person against a bot. You choose the bot: **Random** (fires at random cells) or **Hunt-Target** (hunts at random, then fires at the neighbours of a hit until the ship sinks). |
| AI vs AI | Watch a Random bot play a Hunt-Target bot. |

The browser offers the same two bots, plus online play between two people.

## How to play

- The board is 10 x 10. Each player has five ships: Carrier (5), Battleship (4), Cruiser (3), Submarine (3), Destroyer (2).
- Cells are written as a row letter and a column number, for example `D-4` (`D4` and `d4` work too). Rows are `A`-`J`, columns are `1`-`10`.
- **Placing ships**: choose *Manually* or *Randomly*. When placing manually, give the first cell of each ship and a direction. *Horizontal* extends to the right, *Vertical* extends downward. Ships must stay on the board and must not overlap.
- **Turns**: players alternate and fire one shot per turn. A shot is `Hit`, `Miss`, or `Sunk` (the ship's last cell was hit). A target outside the board, or a cell already fired at, is `Invalid` and you shoot again.
- **Winning**: sink all five enemy ships.
- Boards use `~` water, `#` ship, `X` hit, `o` miss. The enemy board only ever shows hits and misses.

Shots and the winner are also written to a log file next to the executable, one JSON object per line:
`Battleship.Console/bin/<Configuration>/net8.0/logs/battleship-<date>.log`.

## Project structure

```
Battleship/
├── battleship.v3.mmd        class diagram (Mermaid), approved: v2 plus TakeTurn(Coordinate) for the web
├── battleship.v2.mmd        the previous version of the class diagram
├── WEB_DESIGN.md            design of the web version: endpoints, events, DTOs, session rules
├── Battleship.Core/         game logic, no UI
│   ├── Enums/               ShipType, Orientation, GameState, ShotResult, CellState
│   ├── Models/              Coordinate, Ship, Cell, Board, Fleet
│   ├── Interfaces/          IShip, ICell, IBoard, IFleet, IPlayer, IShotStrategy, ICoordinateInput
│   ├── Players/             Player (abstract), HumanPlayer, RandomAIPlayer, HuntTargetAIPlayer
│   └── GameController*.cs   the game facade, split by stage: Setup, Turn, Rules
├── Battleship.Console/      terminal app (Spectre.Console, Serilog logging)
│   ├── Program.cs           composition root: creates every object, including the ships
│   └── Rendering/           board drawing
├── Battleship.Api/          ASP.NET Core API: games against a bot or between two people
│   ├── Contracts/           request, response, and event types (the enemy board type cannot describe a ship)
│   ├── Games/               GameSession, GameStore, OnlinePlayer, Seat, ViewMapper, EmoteCatalog, error handling
│   ├── Hubs/                the SignalR hub, hub authentication, and the sink that sends events to players
│   └── Controllers/         GamesController (REST) and the Result-to-HTTP mapping
├── Battleship.Tests/        NUnit tests for Battleship.Core
├── Battleship.Api.Tests/    NUnit integration tests for the API (WebApplicationFactory, SignalR clients)
└── battleship-web/          React front end (Vite, TypeScript), with Vitest and Playwright tests
```

## Design

- **`GameController` is the only place with game rules**: ship validation and placement, shots, turns, the winner. A UI only talks to it.
- **Data classes hold data only**: `Ship`, `Cell`, `Board`, `Fleet`, and `Coordinate` have no rules apart from `Ship.IsSunk` and `Cell.IsOccupied`.
- **Players shoot through `IShotStrategy`** (`SelectTarget`, `ReceiveResult`). `HumanPlayer` asks an `ICoordinateInput`, so Core never touches the console. Bots only know the results of their own shots; they never look at the enemy board.
- **Dependencies are injected through constructors.** `Program.cs` builds the players, boards, fleets, and ships and hands them to `GameController`.
- **Events**: `OnShotResolved` and `OnGameEnded`. The console subscribes to them for messages and logging; Core never writes to the screen.
- **Randomness is injected** (`Random`), so games and ship placement are repeatable with a seed.

The web version adds these rules on top of Core (details in [WEB_DESIGN.md](WEB_DESIGN.md)):

- **The server decides everything.** A browser only says "fire at D-4". Commands go over REST; SignalR only pushes events from the server to the browsers.
- **One lock per game.** Every command, and every timer, runs inside `GameSession` under one lock. The events it produces are sent after the lock is released.
- **Each player sees only their own view.** `ViewMapper` builds a `GameView` per seat. The enemy board type has no value for an unhit ship, so it cannot leak where the ships are.
- **A player is identified by a random token** that the server gives them for one game (header `X-Player-Token`, or the `access_token` query value for the SignalR connection). It is an opaque token that the server looks up in its own memory, not a JWT, and there is no login. A JWT would pay off with accounts, or when servers cannot share state; here the token lives only as long as one game and every request already needs the game in memory.
- **Every error has the same shape** (RFC 7807 problem details), including errors raised by the framework.

The class diagram is in [battleship.v3.mmd](battleship.v3.mmd). It covers `Battleship.Core`, including the enumerations and the `Coordinate` struct; the console and API classes are outside it.

## Logging

Both apps log with [Serilog](https://serilog.net). Our own code calls `Serilog.ILogger` directly; the framework (ASP.NET Core, SignalR) logs through .NET's `ILogger`, and `UseSerilog` forwards that to the same place.

- **File**: one JSON object per line (`CompactJsonFormatter`). The message template is kept as `@mt` and every value is its own field: `PlayerName`, `Target` (with `X` and `Y`), `Result`, `GameCode`, `RequestId`, `EventName`, and so on. The API writes `logs/battleship-api-<date>.log` relative to the working directory and keeps 31 files. Player tokens are never logged.
- **Console**: the API also prints plain text, for example `[11:04:34 INF] Ann fired at Coordinate {X=0, Y=0}: Miss`.
- **Requests**: one line per HTTP request (`HTTP POST /api/games/ABCDE/shots responded 200 in 3.2 ms`). A rejected request also logs the reason (`EventName` `RequestRejected`).
- **Configuration**: the `Serilog` section of `Battleship.Api/appsettings.json` (level, sinks, path, retention). Any value can be overridden with an environment variable, for example `Serilog__MinimumLevel__Default=Warning`.
- `GameController` does not log. It raises `OnShotResolved` and `OnGameEnded`; `GameSession` (API) and `GameEventReporter` (console) write the entries.

## Tests

```
dotnet test Battleship.Tests
```

212 test cases (166 test methods; a method with several `[TestCase]` attributes runs once per case), all passing:

| Area | Test methods |
|---|---|
| `Ship`, `Cell`, `Board`, `Fleet` | 20 |
| `GameController` (setup, random placement, shots, turns, end of game) | 113 |
| `HumanPlayer`, `RandomAIPlayer`, `HuntTargetAIPlayer` | 27 |
| Full AI games (Random vs Random, Hunt vs Hunt, mixed) and the bot comparison | 6 |

Coverage of `GameController`, measured only by the tests in `Battleship.Tests/GameController` (141 cases): **100% of lines (207 of 207), 100% of branches (72 of 72), 100% of methods**. Run `./coverage-gamecontroller.ps1` for the numbers and a per-line HTML report in `coverage/gamecontroller/index.html`. Coverage says which lines run, not that every case is checked; the edge cases are listed in `GameControllerEdgeCaseTests`.

Coverage of `Battleship.Core` as a whole (from `dotnet test Battleship.Tests --collect:"XPlat Code Coverage"`): **100% of lines and 100% of branches**. `Battleship.Console` has no automated tests; it was checked by playing all three modes.

### API tests

```
dotnet test Battleship.Api.Tests
```

218 test cases (194 test methods), all passing. They start the API in memory and talk to it over HTTP (and over SignalR for online games) with seeded randomness, so a run is repeatable. Test classes run in parallel (each has its own in-memory server); `LoadTests` runs alone so its timings are not disturbed by other tests.

| Area | Test methods |
|---|---|
| Creating a game and its validation | 18 |
| Reading a game and the token check | 9 |
| Placing ships by hand and at random | 23 |
| Firing, bot replies, invalid shots | 16 |
| Whole games over HTTP, both bots | 5 |
| Hidden information: the enemy board never shows a ship | 7 |
| Sunk ships on the enemy board | 7 |
| Error format and logging | 5 |
| The JSON log file: one object per line, expected fields, no player tokens, unhandled exceptions | 3 |
| Online games: rooms, joining, two players | 20 |
| SignalR hub and its events | 18 |
| Emotes | 14 |
| Turn limit and absent players | 12 |
| Classes behind the endpoints (`GameSession`, `GameStore`, `OnlinePlayer`, `Result`) | 24 |
| The in-memory log sink the tests read logs with | 10 |
| Load: many games at once, two players firing at the same moment, SignalR delivery | 3 |

Coverage of `Battleship.Api`: 98.5% of lines and 94.3% of branches.

#### Load tests

`LoadTests` checks that the API stays correct with many games at once, and prints how fast it is:

- **Many bot games at once.** Every game is played to the end over HTTP, and every request is timed.
- **Two players firing at the same moment.** The server must serve them one at a time: no error other than 409 ("not your turn"), the shots of the two players differ by at most one, and the number of shots the server counted equals the number it accepted.
- **Many online games at once.** Each has two SignalR connections. Every shot must reach both players exactly once, each with its own view version.

```
dotnet test Battleship.Api.Tests --filter "FullyQualifiedName~LoadTests" --logger "console;verbosity=detailed"
```

The size of the run can be changed with `LOAD_GAMES`, `LOAD_CONCURRENCY`, and `LOAD_ONLINE_GAMES`. The API runs in memory inside the test process (no network), and the test client shares the CPU with it, so the numbers show the cost of the API itself and not of a real network. Median of three runs per row, in Release on a laptop with 12 logical processors, with logging at the default `Information` level:

| Clients at once | Requests per second | Latency p50 | p95 | p99 |
|---|---|---|---|---|
| 1 | 337 | 2.4 ms | 4.3 ms | 6.7 ms |
| 10 | 1,193 | 6.3 ms | 16.4 ms | 26.1 ms |
| 100 | 1,504 | 64.2 ms | 95.5 ms | 127.5 ms |

With 100 clients the machine is saturated, so requests wait in a queue: latency is about the number of clients divided by the requests per second (100 / 1,504 is about 66 ms). A single request takes about 2 ms. Logging is a large part of that cost: with the level set to `Warning` (`Serilog__MinimumLevel__Default=Warning`) the same 100 clients reach about 3,200 requests per second, so logging every request and shot costs about half the throughput. Fifty online games (100 SignalR connections) delivered 1,000 events in about 2.2 s. A finished game keeps about 38 KB in memory (32 KB at `Warning`), and finished games are not removed yet (see [Planned](#planned)).

### Front end tests

```
cd battleship-web
npm test        # component and unit tests (Vitest)
npm run e2e     # browser tests (Playwright) against the real API
```

195 Vitest tests cover the API client, the saved session, the placement preview, every component and screen, the turn countdown, the `useGame` hook (against a bot and online), and whole games against a scripted server.

13 Playwright tests drive a real browser against the real API:

- Against a bot: full games against both bots, placing ships by hand, rotating with the R key, resuming after a reload, and starting over.
- Online, with two browser contexts: a whole game, a shot showing on the other screen at once, reloading in the middle of a battle, seeing the opponent drop out and come back, refusing an unknown code or a full room, and leaving the lobby.
- Turn limit: a player who does nothing loses. This test skips itself unless the API runs with a short limit, for example `Game__TurnTimeout=00:00:06 E2E_TURN_TIMEOUT=1 npx playwright test e2e/turn-timeout.spec.ts`, so 12 of the 13 run by default.

Online play has been tried with two browsers on one machine, not yet over two real networks.

## Planned

| Step | What |
|---|---|
| Hardening | A rate limit (only emotes are limited now), cleanup of finished and abandoned games, and a limit on the number of games |
| Scaling | State outside the process and a SignalR backplane (for example Redis) before running more than one server |
| Hosting | A Docker image that serves the API and the built React files, a free host, and a CI build |
| Optional | Rematch, Salvo mode (shots per turn equal to the ships still afloat), a database |

See [TODO.md](TODO.md) for the full list.
