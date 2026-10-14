# Othello v2

Othello (Reversi) implementation in C# targeting .NET 8. The reusable game
model is contained in `OthelloV2.Core`; the console application is a thin
client, and the test project verifies the Core behavior.

## Build

From the repository root:

```powershell
dotnet build "Project\IndividualProject\Othello.v2\src\Othello.Core\OthelloV2.Core.csproj"
dotnet build "Project\IndividualProject\Othello.v2\src\Othello.ConsoleApp\OthelloV2.ConsoleApp.csproj"
dotnet build "Project\IndividualProject\Othello.v2\tests\Othello.Core.Tests\OthelloV2.Core.Tests.csproj"
```

## Run

```powershell
dotnet run --project "Project\IndividualProject\Othello.v2\src\Othello.ConsoleApp\OthelloV2.ConsoleApp.csproj"
```

## Test

```powershell
dotnet test "Project\IndividualProject\Othello.v2\tests\Othello.Core.Tests\OthelloV2.Core.Tests.csproj"
```

## Layout

```text
src/Othello.Core/              Reusable game model and rules
src/Othello.ConsoleApp/        Thin console client
tests/Othello.Core.Tests/      Core unit tests and test helpers
docs/                          Design diagrams
```

## Design decisions

- `OthelloV2.Core` has no project dependencies and does not use
  `System.Console`, so it can be reused by a future Web API and React client.
- Game rules and state transitions are kept in private methods of
  `GameManager`; fixed rules do not require validator or rule interfaces.
- Data-holder classes remain free of game logic, while automated players may
  contain player decision logic.
- Public board state is read-only to consumers and internally writable.
- Nullable reference types and implicit usings are enabled. Types use
  file-scoped namespaces and one type per file.
