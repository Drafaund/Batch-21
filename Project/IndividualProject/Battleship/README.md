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

Shots and the winner are also written to a log file next to the executable:
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
├── Battleship.Tests/        xUnit tests for Battleship.Core
├── Battleship.Api.Tests/    integration tests for the API (WebApplicationFactory, SignalR clients)
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

## Tests

```
dotnet test Battleship.Tests
```

177 test cases (142 test methods; theories run several cases each), all passing:

| Area | Test methods |
|---|---|
| `Ship`, `Cell`, `Board`, `Fleet` | 20 |
| `GameController` (setup, random placement, shots, turns, end of game) | 89 |
| `HumanPlayer`, `RandomAIPlayer`, `HuntTargetAIPlayer` | 27 |
| Full AI games (Random vs Random, Hunt vs Hunt, mixed) and the bot comparison | 6 |

Coverage of `Battleship.Core` (from `dotnet test Battleship.Tests --collect:"XPlat Code Coverage"`): **100% of lines and 100% of branches**. `Battleship.Console` has no automated tests; it was checked by playing all three modes.

### API tests

```
dotnet test Battleship.Api.Tests
```

186 test cases (165 test methods), all passing. They start the API in memory and talk to it over HTTP (and over SignalR for online games) with seeded randomness, so a run is repeatable.

| Area | Test methods |
|---|---|
| Creating a game and its validation | 18 |
| Reading a game and the token check | 9 |
| Placing ships by hand and at random | 23 |
| Firing, bot replies, invalid shots | 15 |
| Whole games over HTTP, both bots | 5 |
| Hidden information: the enemy board never shows a ship | 7 |
| Sunk ships on the enemy board | 7 |
| Error format and logging | 5 |
| Online games: rooms, joining, two players | 20 |
| SignalR hub and its events | 17 |
| Emotes | 14 |
| Turn limit and absent players | 11 |
| Classes behind the endpoints | 11 |
| Load: many games at once, two players firing at the same moment, SignalR delivery | 3 |

Coverage of `Battleship.Api`: 96.1% of lines and 91.0% of branches.

#### Load tests

`LoadTests` checks that the API stays correct with many games at once, and prints how fast it is:

- **Many bot games at once.** Every game is played to the end over HTTP, and every request is timed.
- **Two players firing at the same moment.** The server must serve them one at a time: no error other than 409 ("not your turn"), the shots of the two players differ by at most one, and the number of shots the server counted equals the number it accepted.
- **Many online games at once.** Each has two SignalR connections. Every shot must reach both players exactly once, each with its own view version.

```
dotnet test Battleship.Api.Tests --filter "FullyQualifiedName~LoadTests" --logger "console;verbosity=detailed"
```

The size of the run can be changed with `LOAD_GAMES`, `LOAD_CONCURRENCY`, and `LOAD_ONLINE_GAMES`. The API runs in memory inside the test process (no network), and the test client shares the CPU with it, so the numbers show the cost of the API itself and not of a real network. One run in Release on a laptop with 12 logical processors:

| Clients at once | Requests per second | Latency p50 | p95 | p99 |
|---|---|---|---|---|
| 1 | 627 | 1.1 ms | 2.9 ms | 4.3 ms |
| 10 | 1,662 | 3.9 ms | 11.3 ms | 25.0 ms |
| 100 | 2,178 | 40.3 ms | 85.5 ms | 117.1 ms |

With 100 clients the machine is saturated, so requests wait in a queue: latency is about the number of clients divided by the requests per second (100 / 2,178 is about 46 ms). A single request still takes about a millisecond. Fifty online games (100 SignalR connections) delivered 1,000 events in 1.9 s. A finished game keeps about 22 KB in memory, and finished games are not removed yet (see [Planned](#planned)).

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
