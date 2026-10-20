# Battleship

A two-player Battleship game written in C# (.NET 8). It runs in the terminal and supports human and computer players.
The game rules live in a UI-free library (`Battleship.Core`), so the same code can later sit behind a web API.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An interactive terminal (the menus use arrow keys, so piped input is not supported)

## Run

From this folder (`Battleship`):

```
dotnet run --project Battleship.Console
```

## Game modes

| Mode | Description |
|---|---|
| Human vs Human | Two people share one keyboard. The screen is cleared between turns so nobody sees the other's fleet. |
| Human vs AI | One person against a bot. You choose the bot: **Random** (fires at random cells) or **Hunt-Target** (hunts at random, then fires at the neighbours of a hit until the ship sinks). |
| AI vs AI | Watch a Random bot play a Hunt-Target bot. |

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
├── battleship.v2.mmd        class diagram (Mermaid)
├── Battleship.Core/         game logic, no UI
│   ├── Enums/               ShipType, Orientation, GameState, ShotResult, CellState
│   ├── Models/              Coordinate, Ship, Cell, Board, Fleet
│   ├── Interfaces/          IShip, ICell, IBoard, IFleet, IPlayer, IShotStrategy, ICoordinateInput
│   ├── Players/             Player (abstract), HumanPlayer, RandomAIPlayer, HuntTargetAIPlayer
│   └── GameController*.cs   the game facade, split by stage: Setup, Turn, Rules
├── Battleship.Console/      terminal app (Spectre.Console, Serilog logging)
│   ├── Program.cs           composition root: creates every object, including the ships
│   └── Rendering/           board drawing
└── Battleship.Tests/        xUnit tests for Battleship.Core
```

## Design

- **`GameController` is the only place with game rules**: ship validation and placement, shots, turns, the winner. A UI only talks to it.
- **Data classes hold data only**: `Ship`, `Cell`, `Board`, `Fleet`, and `Coordinate` have no rules apart from `Ship.IsSunk` and `Cell.IsOccupied`.
- **Players shoot through `IShotStrategy`** (`SelectTarget`, `ReceiveResult`). `HumanPlayer` asks an `ICoordinateInput`, so Core never touches the console. Bots only know the results of their own shots; they never look at the enemy board.
- **Dependencies are injected through constructors.** `Program.cs` builds the players, boards, fleets, and ships and hands them to `GameController`.
- **Events**: `OnShotResolved` and `OnGameEnded`. The console subscribes to them for messages and logging; Core never writes to the screen.
- **Randomness is injected** (`Random`), so games and ship placement are repeatable with a seed.

The class diagram is in [battleship.v2.mmd](battleship.v2.mmd). It covers `Battleship.Core`; the console classes are outside it.

## Tests

```
dotnet test Battleship.Tests
```

161 test cases (129 test methods; theories run several cases each), all passing:

| Area | Test methods |
|---|---|
| `Ship`, `Cell`, `Board`, `Fleet` | 20 |
| `GameController` (setup, random placement, shots, turns, end of game) | 76 |
| `HumanPlayer`, `RandomAIPlayer`, `HuntTargetAIPlayer` | 27 |
| Full AI games (Random vs Random, Hunt vs Hunt, mixed) and the bot comparison | 6 |

Coverage of `Battleship.Core` (from `dotnet test Battleship.Tests --collect:"XPlat Code Coverage"`): **100% of lines and 100% of branches**. `Battleship.Console` has no automated tests; it was checked by playing all three modes.

## Planned

A web version: an ASP.NET Core API on top of `Battleship.Core` and a React front end.
