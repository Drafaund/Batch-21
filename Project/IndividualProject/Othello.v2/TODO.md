# Othello.v2 - Implementation TODO

This file is the work order for an AI agent. Read sections 0 and 1 fully before touching any file.
Work phase by phase. After every phase: build, run the tests that exist, tick the boxes, and stop if something fails.

---

## 0. Context and rules (read first)

**What we are building:** an Othello (Reversi) game in C#. Only `Othello.Core` is modelled in the class diagram
(`docs/othello_v2_full.mmd`). The console app is a thin client on top of Core. A Web API + React client will come later and must be
able to reuse `Othello.Core` unchanged.

**Locations (Windows):**

```
Git root + solution + .gitignore : D:\Sertifikasi\Formulatrix SE Bootcamp\Batch-21
Project folder (work only here)  : D:\Sertifikasi\Formulatrix SE Bootcamp\Batch-21\Project\IndividualProject\Othello.v2
```

The paths contain spaces. Always quote them. All paths in this file are relative to the **project folder** unless stated otherwise.

**Hard rules**

1. The solution file and `.gitignore` already exist at the Git root. Do **not** create another `.sln`. Do **not** edit other projects in that solution.
   Do **not** edit `.gitignore` unless `bin/`, `obj/` or `.vs/` are not ignored (then only append those lines).
2. Implement the class diagram **exactly**. Do not add public types or public members that are not in the diagram. Private/internal helpers are allowed.
3. `Othello.Core` must not reference any other project and must never use `System.Console`.
4. All game logic (move validation, flipping, pass, game end) lives **inside `GameManager`** as private methods. The rules are fixed, so there are no validator or rule interfaces (KISS).
5. Except for `GameManager` and the automated players, classes are data holders: no game logic in them.
6. Naming: `PascalCase` for types, methods, properties, events. `camelCase` for locals and parameters. Private fields use `_camelCase`.
   Interfaces start with `I`. Method names start with a verb (`Is...`/`Has...` allowed for `bool`). Classes are singular nouns.
7. Visibility: board state is public read, internal write (`{ get; internal set; }`). Everything that can be private is private. Classes are `sealed` unless they are a base class.
8. Language level: nullable reference types enabled, implicit usings enabled, one type per file, file name equals type name, file-scoped namespaces.
9. Code, comments and identifiers are in English.
10. Commit once per phase with the suggested message. Do not push.

---

## 1. Target structure

```
Batch-21/                                   (git root)
|- <existing>.sln                           (already exists)
|- .gitignore                               (already exists)
`- Project/IndividualProject/Othello.v2/
   |- README.md
   |- TODO.md
   |- .editorconfig
   |- docs/
   |  |- othello_v2_full.mmd                (copied in manually by the owner)
   |  `- slides/                            (optional, owner puts slide diagrams here)
   |- src/
   |  |- Othello.Core/
   |  |  |- Othello.Core.csproj
   |  |  |- GameManager.cs
   |  |  |- Enums/        PieceColor.cs, GameResult.cs, MoveFailureReason.cs
   |  |  |- Models/       Position.cs, Direction.cs, Score.cs, Piece.cs, Cell.cs, Board.cs, Move.cs, MoveResult.cs
   |  |  `- Players/      Player.cs, HumanPlayer.cs, IAutoPlayer.cs, RandomAIPlayer.cs, GreedyAIPlayer.cs
   |  `- Othello.ConsoleApp/
   |     |- Othello.ConsoleApp.csproj
   |     |- Program.cs
   |     |- GameConsolePresenter.cs
   |     `- ConsoleBoardRenderer.cs
   `- tests/
      `- Othello.Core.Tests/
         |- Othello.Core.Tests.csproj
         |- BoardTests.cs
         |- GameManagerStartTests.cs
         |- GameManagerMoveTests.cs
         |- GameManagerFlipTests.cs
         |- GameManagerTurnTests.cs
         |- GameManagerEventTests.cs
         |- AutoPlayerTests.cs
         `- TestHelpers/  BoardBuilder.cs, ScriptedAutoPlayer.cs
```

Project names: `Othello.Core`, `Othello.ConsoleApp`, `Othello.Core.Tests`.
(`ConsoleApp` and not `Console`, so `Console.WriteLine` never resolves to a namespace.)
If a project with one of these names already exists in the solution (for example from an earlier version), prefix all three with `OthelloV2.` instead
(`OthelloV2.Core`, ...) and use that prefix for namespaces too. Note the decision in `README.md`.

---

## 2. Phase 1 - Scaffold

- [x] Run `dotnet --list-sdks`. Pick the target framework: the same as other projects in the solution, otherwise the newest LTS installed. Available SDK: `8.0.425`; target framework: `net8.0`.
- [x] Find the solution: `Get-ChildItem "D:\Sertifikasi\Formulatrix SE Bootcamp\Batch-21" -Filter *.sln`. Solution: `Batch21.sln`.
- [x] Check name collisions with `dotnet sln "<sln>" list`. Existing `Othello.Core` and `Othello.ConsoleApp` projects require the `OthelloV2.` prefix for all three new project names.
- [x] Create the three projects (PowerShell, adjusted to `net8.0` and the required `OthelloV2.` prefix):

```powershell
$batch = "D:\Sertifikasi\Formulatrix SE Bootcamp\Batch-21"
$proj  = "$batch\Project\IndividualProject\Othello.v2"
Set-Location $proj
dotnet new classlib -n OthelloV2.Core         -o src\Othello.Core --framework net8.0
dotnet new console  -n OthelloV2.ConsoleApp   -o src\Othello.ConsoleApp --framework net8.0
dotnet new xunit    -n OthelloV2.Core.Tests   -o tests\Othello.Core.Tests --framework net8.0
dotnet add src\Othello.ConsoleApp reference src\Othello.Core
dotnet add tests\Othello.Core.Tests reference src\Othello.Core
```

- [x] Add them to the **existing** solution, grouped in the `Project\IndividualProject\Othello.v2` solution folder:

```powershell
$sln = "<full path of the existing .sln>"
dotnet sln $sln add --solution-folder "Project\IndividualProject\Othello.v2" `
  "$proj\src\Othello.Core\Othello.Core.csproj" `
  "$proj\src\Othello.ConsoleApp\Othello.ConsoleApp.csproj" `
  "$proj\tests\Othello.Core.Tests\Othello.Core.Tests.csproj"
```

- [x] Delete `Class1.cs` and `UnitTest1.cs`. Create the empty folders `Enums`, `Models`, `Players`, `TestHelpers`, `docs`.
- [x] In all three `.csproj`: `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>.
- [x] In `Othello.Core.csproj` add `InternalsVisibleTo` for `OthelloV2.Core.Tests`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Othello.Core.Tests" />
</ItemGroup>
```

- [x] Create `.editorconfig` in the project folder with the content of Appendix D.
- [x] Create a short `README.md` (what it is, how to build, run, test, project layout, design decisions from section 0).
- [x] Verify: `dotnet build` on each of the three `.csproj` succeeds (build the projects, not the whole solution, so other projects cannot break it). Core, ConsoleApp, and Tests all build successfully in sequential runs.

Commit: `chore: scaffold Othello.v2 projects`

---

## 3. Phase 2 - Core data types

Use the signatures in Appendix A. One type per file, in the folder shown in section 1.

- [x] `Enums/PieceColor.cs`, `Enums/GameResult.cs`, `Enums/MoveFailureReason.cs`
- [x] `Models/Position.cs`, `Direction.cs`, `Score.cs` (`readonly record struct`)
- [x] `Models/Piece.cs`, `Cell.cs`, `Board.cs`
- [x] `Models/Move.cs`, `MoveResult.cs` (`sealed record`; `IsSuccess` is derived, never stored)
- [x] `Board`: size 8, `Cells` has 64 items in row-major order, `Cells[row * Size + col].Position == new Position(row, col)`, all cells start empty (`Piece` is null).
- [x] Verify: build passes.

Commit: `feat(core): add enums and data models`

---

## 4. Phase 3 - Players

- [x] `Players/Player.cs` abstract: `Color`, `Name`, protected constructor `(PieceColor color, string name)`.
- [x] `Players/HumanPlayer.cs`: no extra members.
- [x] `Players/IAutoPlayer.cs`: `Move ChooseMove(IReadOnlyList<Move> candidates);`
- [x] `Players/RandomAIPlayer.cs`: `: Player, IAutoPlayer`. Private `_random`. Constructor accepts an optional `Random` (so tests can pass a seeded one).
- [x] `Players/GreedyAIPlayer.cs`: `: Player, IAutoPlayer`. Picks the candidate with the most `Flipped` positions. On a tie, the first one.
- [x] Contract of `ChooseMove`: precondition `candidates` is never empty. Postcondition: result is one of `candidates`. The AI players need no board and no validator.
- [x] Verify: build passes.

Commit: `feat(core): add players and automated strategies`

---

## 5. Phase 4 - GameManager

Implement exactly the members in Appendix A. Follow Appendix B (rules and behaviour) and Appendix C (event order).

- [x] Fields, constructors (public + `internal` for tests), properties.
- [x] Private helpers: `IsInside`, `GetFlips`, `FindValidMoves`, `HasValidMove`, `TryGetResult`, `ApplyMove`, `AdvanceTurn`, `PlayAutoTurns`.
- [x] Public API: `IsGameOver`, `Start`, `TryPlayMove`, `GetValidMoves`, `GetScore`.
- [x] Events: `GameStarted`, `MovePlayed`, `ScoreChanged`, `TurnChanged`, `TurnSkipped`, `GameOver` (all `Action` / `Action<T>`).
- [x] Re-entrancy guard with `_isProcessing` (use `try/finally`).
- [x] Verify: build passes; no `Console` usage in Core: `Select-String -Path src\Othello.Core -Pattern "Console\." -Recurse` returns nothing.

Commit: `feat(core): implement GameManager`

---

## 6. Phase 5 - Tests (xUnit)

Test names: `MethodName_Condition_ExpectedResult`. Test through the public API of `GameManager` (internal members are visible through `InternalsVisibleTo`).

Helpers (`TestHelpers/`):

- [x] `BoardBuilder`: builds a `Board` from 8 strings of 8 characters (`.` empty, `B` black, `W` white).
- [x] `ScriptedAutoPlayer : Player, IAutoPlayer`: returns a predefined sequence of moves; can be configured to return a move that is not in `candidates`.

Tests:

- [x] `BoardTests`: 64 cells, row-major positions, all empty on a new board.
- [x] `GameManagerStartTests`: initial pieces are W at (3,3) and (4,4), B at (3,4) and (4,3); `Start` succeeds and returns `None`; second `Start` returns `AlreadyStarted`;
      `TryPlayMove` before `Start` returns `NotStarted`; constructor rejects players with the wrong color.
- [x] `GameManagerMoveTests`: one test for each failure: `OutOfBounds`, `CellOccupied`, `NoPiecesFlipped`, `GameOver`, `ReentrantCall`
      (call `TryPlayMove` from inside a `MovePlayed` handler and capture the result). Failed moves leave the board and the turn unchanged.
- [ ] `GameManagerFlipTests`: initial valid moves for Black are exactly (2,3), (3,2), (4,5), (5,4). Playing (2,3) flips (3,3) and the score becomes Black 4, White 1.
      Flipping works in each of the 8 directions and in several directions at once.
- [ ] `GameManagerTurnTests`: turns alternate; pass (opponent has no valid move, current player moves again); game over when neither player can move;
      `Result` matches the final score (`BlackWins`, `WhiteWins`, `Draw`); `GetValidMoves` is empty before `Start` and after game over.
- [ ] `GameManagerEventTests`: record the event order and compare with Appendix C for: start, a normal move, a pass, the final move.
- [ ] `AutoPlayerTests`: Greedy picks the candidate with the most flips; Random with a fixed seed is deterministic and always returns one of the candidates;
      `GameManager` throws `InvalidOperationException` when an `IAutoPlayer` returns a move that is not a candidate; an AI-vs-AI game with several seeds always ends
      with `IsGameOver() == true` and a consistent `Result`.
- [ ] Verify: `dotnet test tests\Othello.Core.Tests` is all green.

Commit: `test(core): add unit tests for GameManager and players`

---

## 7. Phase 6 - Console app

Console is a thin client. Do not put game rules here.

- [x] `ConsoleBoardRenderer`: `Render(Board board)` prints a header `a b c d e f g h`, rows `1`..`8`, and `.`, `B`, `W` for empty/black/white.
- [x] `GameConsolePresenter`: constructor `(Board board, ConsoleBoardRenderer renderer)`; `Subscribe(GameManager)` and `Unsubscribe(GameManager)` hook the six events.
      Private handlers: `HandleGameStarted`, `HandleMovePlayed`, `HandleScoreChanged`, `HandleTurnChanged`, `HandleTurnSkipped`, `HandleGameOver`.
      `HandleMovePlayed` prints who moved, where, how many pieces were flipped, then renders the board.
- [x] `Program`: `Main`, `BuildGame` (menu: Human vs Human, Human vs Random AI, Human vs Greedy AI, AI vs AI), `ParsePosition`.
      Input format is `<column letter><row number>`, for example `d3` means `Row = 2, Col = 3`. Invalid text asks again.
      Game loop: `Start()`, then while `!IsGameOver()` read input for the current human player, call `TryPlayMove`, and print a readable message for each `MoveFailureReason`.
      AI turns happen inside `Start`/`TryPlayMove`; the presenter shows them through events. Unsubscribe at the end.
- [x] Verify: `dotnet run --project src\Othello.ConsoleApp` and play a short game in each mode; an AI-vs-AI game runs to the end.

Commit: `feat(console): add console client`

---

## 8. Phase 7 - Final checks

- [x] `dotnet build` for the three projects has 0 errors and 0 warnings from naming rules.
- [x] `dotnet test` is green.
- [x] Core has no reference to Console or other projects: check `Othello.Core.csproj` and run the `Select-String` check from Phase 4.
- [x] Every private field in the repo starts with `_` (search `private ` declarations).
- [ ] Public API of Core equals the diagram: no extra public types or members.
- [ ] Copy `othello_v2_full.mmd` into `docs/` if the owner has not done it yet.
- [ ] `git status` shows only files inside the project folder (plus the `.sln` change). Nothing from `bin/` or `obj/`.

Commit: `docs: finalize README and checks`

### Definition of done (assignment)

- [ ] OOP: encapsulation (private state, internal setters), abstraction (`Player`, `IAutoPlayer`), inheritance (`Player` hierarchy), polymorphism (`IAutoPlayer.ChooseMove`).
- [ ] SOLID where it fits: `IAutoPlayer` strategies (OCP, LSP, DIP), data classes without logic (SRP for data), documented KISS decision for the fixed rules.
- [ ] At least one Action delegate / event: six events on `GameManager`.
- [ ] Code matches `docs/othello_v2_full.mmd`.

---

## Appendix A - API contract

```csharp
namespace Othello.Core;

public enum PieceColor { Black, White }
public enum GameResult { BlackWins, WhiteWins, Draw }
public enum MoveFailureReason
{
    None, NotStarted, AlreadyStarted, GameOver, OutOfBounds, CellOccupied, NoPiecesFlipped, ReentrantCall
}

public readonly record struct Position(int Row, int Col);
public readonly record struct Direction(int DRow, int DCol);
public readonly record struct Score(int Black, int White);

public sealed class Piece
{
    internal Piece(PieceColor color);
    public PieceColor Color { get; internal set; }
}

public sealed class Cell
{
    internal Cell(Position position);
    public Position Position { get; }
    public Piece? Piece { get; internal set; }          // null = empty cell
}

public sealed class Board
{
    internal Board();                                    // 8x8, all cells empty
    public int Size { get; }
    public IReadOnlyList<Cell> Cells { get; }            // Size * Size items, row-major
}

public sealed record Move(Position Target, PieceColor Color, IReadOnlyList<Position> Flipped);

public sealed record MoveResult(MoveFailureReason Failure, IReadOnlyList<Move> Moves)
{
    public bool IsSuccess => Failure == MoveFailureReason.None;
}

public abstract class Player
{
    protected Player(PieceColor color, string name);
    public PieceColor Color { get; }
    public string Name { get; }
}

public sealed class HumanPlayer : Player { public HumanPlayer(PieceColor color, string name); }

public interface IAutoPlayer
{
    Move ChooseMove(IReadOnlyList<Move> candidates);
}

public sealed class RandomAIPlayer : Player, IAutoPlayer { public RandomAIPlayer(PieceColor color, string name, Random? random = null); /* private Random _random */ }
public sealed class GreedyAIPlayer : Player, IAutoPlayer { public GreedyAIPlayer(PieceColor color, string name); }

public sealed class GameManager
{
    public GameManager(Player blackPlayer, Player whitePlayer);
    internal GameManager(Player blackPlayer, Player whitePlayer, Board board, PieceColor firstTurn = PieceColor.Black);

    public Board Board { get; }
    public PieceColor CurrentTurn { get; private set; }
    public GameResult? Result { get; private set; }

    public event Action? GameStarted;
    public event Action<Move>? MovePlayed;
    public event Action<Score>? ScoreChanged;
    public event Action<PieceColor>? TurnChanged;
    public event Action<PieceColor>? TurnSkipped;
    public event Action<GameResult>? GameOver;

    public bool IsGameOver();
    public MoveResult Start();
    public MoveResult TryPlayMove(Position position);
    public IReadOnlyList<Position> GetValidMoves();
    public Score GetScore();

    // private: _blackPlayer, _whitePlayer, _directions (8 items), _isStarted, _isProcessing,
    // PlayAutoTurns(List<Move>), ApplyMove(Position, PieceColor), AdvanceTurn(), FindValidMoves(PieceColor),
    // GetFlips(Position, PieceColor), HasValidMove(PieceColor), TryGetResult(out GameResult), IsInside(Position)
}
```

The public `GameManager` constructor creates a new `Board` and places the four starting pieces.
The `internal` constructor takes a ready-made board (used by tests) and does not place any pieces.
Both constructors throw if a player is null or has the wrong color (`blackPlayer.Color` must be Black, `whitePlayer.Color` White).

## Appendix B - Rules and behaviour

**Board:** 8x8. `Position(Row, Col)` is zero-based, `Row 0` is the top row. Cell for a position is `Cells[Row * Size + Col]`.

**Start position:** White at (3,3) and (4,4). Black at (3,4) and (4,3). Black moves first.

**Directions:** the 8 neighbours, `(-1,-1) (-1,0) (-1,1) (0,-1) (0,1) (1,-1) (1,0) (1,1)`.

**Flips for a move at P by color C:** for each direction, walk from P while the next cell is inside the board and holds an opponent piece, collecting positions.
If the walk stops on a cell that holds a piece of color C and at least one position was collected, those positions are flipped. A move is valid only if
the target cell is empty and the total number of flips is at least 1.

**Applying a move:** place a new `Piece` of color C on the target cell and change `Color` of every flipped piece. The returned `Move` has the target, the color and the flipped positions.

**`FindValidMoves(color)`:** returns one `Move` (with `Flipped` filled) for every empty cell that is a valid move, ordered by row, then column.

**`TryPlayMove(position)` checks, in this order:**
1. `_isProcessing` is true -> `ReentrantCall`.
2. not started -> `NotStarted`.
3. game over -> `GameOver`.
4. position outside the board -> `OutOfBounds`.
5. cell not empty -> `CellOccupied`.
6. no flips -> `NoPiecesFlipped`.
On failure return `MoveResult` with that reason and an empty `Moves` list. State and events are untouched. On success: apply the move, raise events (Appendix C),
advance the turn, then run automated turns. The returned `Moves` contains the human move followed by every automated move that happened.

**`Start()`:** if already started return `AlreadyStarted`. Otherwise set `_isStarted`, raise the start events, then run automated turns if the first player is an `IAutoPlayer`.
The returned `Moves` contains every automated move that happened (it may be empty). `Start` assumes the first player has at least one valid move.

**`AdvanceTurn()` after a move by color C, with opponent O:**
1. If neither player has a valid move: set `Result` from the score (more pieces wins, equal is `Draw`), raise `GameOver`. No `TurnChanged`.
2. Else if O has a valid move: `CurrentTurn = O`, raise `TurnChanged(O)`.
3. Else (O must pass): raise `TurnSkipped(O)`, `CurrentTurn` stays C, raise `TurnChanged(C)`.

**`PlayAutoTurns`:** while the game is not over and the player whose turn it is implements `IAutoPlayer`: get candidates with `FindValidMoves`, call `ChooseMove`,
verify the result is one of the candidates (otherwise throw `InvalidOperationException`), apply it with the same logic as a human move, and add it to the result list.

**`_isProcessing`:** true for the whole duration of `Start` and `TryPlayMove`, reset in `finally`.

**`GetValidMoves()`:** positions of `FindValidMoves(CurrentTurn)`; empty before `Start` and after game over.
**`GetScore()`:** counts of black and white pieces. **`IsGameOver()`:** `Result.HasValue`.

## Appendix C - Event order (raised synchronously)

| Situation | Order |
|---|---|
| `Start()` | `GameStarted`, `ScoreChanged`, `TurnChanged(first player)` |
| After a normal move | `MovePlayed`, `ScoreChanged`, `TurnChanged(next player)` |
| After a move, opponent must pass | `MovePlayed`, `ScoreChanged`, `TurnSkipped(opponent)`, `TurnChanged(player who moves again)` |
| After the last move | `MovePlayed`, `ScoreChanged`, `GameOver(result)` (no `TurnChanged`) |

`TurnChanged` always announces who moves next. Automated moves raise the same events in the same order.

## Appendix D - .editorconfig

```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0130.severity = none

# private fields: _camelCase
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel
dotnet_naming_rule.private_fields_underscore.severity = warning

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.underscore_camel.capitalization = camel_case

# interfaces start with I
dotnet_naming_rule.interfaces_start_with_i.symbols = interfaces
dotnet_naming_rule.interfaces_start_with_i.style = prefix_i
dotnet_naming_rule.interfaces_start_with_i.severity = warning

dotnet_naming_symbols.interfaces.applicable_kinds = interface

dotnet_naming_style.prefix_i.required_prefix = I
dotnet_naming_style.prefix_i.capitalization = pascal_case
```
